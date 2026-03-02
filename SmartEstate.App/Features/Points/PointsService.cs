using Microsoft.EntityFrameworkCore;
using SmartEstate.Infrastructure.Persistence;
using SmartEstate.Shared.Results;
using SmartEstate.Shared.Errors;
using SmartEstate.Shared.Time;
using SmartEstate.Domain.Entities;
using SmartEstate.App.Common.Abstractions;
using SmartEstate.Domain.Enums;

namespace SmartEstate.App.Features.Points;

public sealed class PointsService
{
    private readonly SmartEstateDbContext _db;
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;

    public PointsService(SmartEstateDbContext db, IClock clock, ICurrentUser currentUser)
    {
        _db = db;
        _clock = clock;
        _currentUser = currentUser;
    }

    private string GetMonthKey(DateTimeOffset dt)
        => dt.ToString("yyyy-MM");

    private async Task<UserPoints> GetOrCreateUserPointsAsync(Guid userId, CancellationToken ct)
    {
        var now = _clock.UtcNow;
        var monthKey = GetMonthKey(now);

        var up = await _db.UserPoints.FirstOrDefaultAsync(x => x.UserId == userId, ct);
        if (up is null)
        {
            up = UserPoints.Create(userId, monthKey);
            _db.UserPoints.Add(up);
        }
        else
        {
            up.EnsureMonth(monthKey);
        }

        return up;
    }

    public async Task<Result> TrySpendAsync(Guid userId, int points, string reason, string refType, Guid? refId, CancellationToken ct = default)
    {
        if (points <= 0) return Result.Ok();

        var up = await GetOrCreateUserPointsAsync(userId, ct);
        var now = _clock.UtcNow;
        var monthKey = GetMonthKey(now);

        var beforeMonthly = up.MonthlyPoints;
        var beforePermanent = up.PermanentPoints;

        var ok = up.TrySpend(points, monthKey);
        if (!ok)
        {
            return Result.Fail(ErrorCodes.Validation, "INSUFFICIENT_POINTS");
        }

        var entry = new PointLedgerEntry
        {
            UserId = userId,
            Delta = -points,
            Reason = reason,
            RefType = refType,
            RefId = refId,
            IsMonthlyBucket = beforeMonthly > 0,
            BalanceMonthlyAfter = up.MonthlyPoints,
            BalancePermanentAfter = up.PermanentPoints,
            Bucket = beforeMonthly > 0 ? "MONTHLY" : "PERMANENT",
            MonthKey = beforeMonthly > 0 ? monthKey : null,
            TxType = reason
        };

        _db.PointLedgerEntries.Add(entry);
        await _db.SaveChangesAsync(true, ct);
        return Result.Ok();
    }

    public async Task<Result> AddPermanentAsync(Guid userId, int points, string reason, string refType, Guid? refId, CancellationToken ct = default)
    {
        if (points <= 0) return Result.Ok();

        var up = await GetOrCreateUserPointsAsync(userId, ct);

        up.AddPermanent(points);

        var entry = new PointLedgerEntry
        {
            UserId = userId,
            Delta = points,
            Reason = reason,
            RefType = refType,
            RefId = refId,
            IsMonthlyBucket = false,
            BalanceMonthlyAfter = up.MonthlyPoints,
            BalancePermanentAfter = up.PermanentPoints,
            Bucket = "PERMANENT",
            MonthKey = null,
            TxType = reason
        };

        _db.PointLedgerEntries.Add(entry);
        await _db.SaveChangesAsync(true, ct);
        return Result.Ok();
    }

    public async Task<Result> GrantMonthlyPointsAsync(string? monthKey = null, CancellationToken ct = default)
    {
        var now = _clock.UtcNow;
        var key = monthKey ?? GetMonthKey(now);

        // Determine month boundaries for idempotency check
        var parts = key.Split('-');
        var year = int.Parse(parts[0]);
        var month = int.Parse(parts[1]);
        var monthStart = new DateTimeOffset(year, month, 1, 0, 0, 0, TimeSpan.Zero);
        var nextMonth = month == 12 ? 1 : month + 1;
        var nextYear = month == 12 ? year + 1 : year;
        var monthEnd = new DateTimeOffset(nextYear, nextMonth, 1, 0, 0, 0, TimeSpan.Zero);

        var users = await _db.Users
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.IsActive)
            .Join(_db.Roles.AsNoTracking(), u => u.RoleId, r => r.Id, (u, r) => new { u.Id, RoleName = r.Name })
            .ToListAsync(ct);

        foreach (var u in users)
        {
            var up = await _db.UserPoints.FirstOrDefaultAsync(x => x.UserId == u.Id, ct);
            if (up is null)
            {
                up = UserPoints.Create(u.Id, key);
                _db.UserPoints.Add(up);
            }

            if (up.MonthlyMonthKey != key)
            {
                up.EnsureMonth(key);
            }

            var amount = 0;
            if (string.Equals(u.RoleName, "Broker", StringComparison.OrdinalIgnoreCase))
                amount = 20;
            else if (string.Equals(u.RoleName, "User", StringComparison.OrdinalIgnoreCase)
                || string.Equals(u.RoleName, "Seller", StringComparison.OrdinalIgnoreCase))
                amount = 3;

            if (amount <= 0) continue;

            // Idempotent: skip if already granted in this month
            var granted = await _db.PointLedgerEntries
                .AsNoTracking()
                .AnyAsync(e =>
                    e.UserId == u.Id
                    && !e.IsDeleted
                    && e.Reason == "GRANT_MONTHLY"
                    && e.CreatedAt >= monthStart
                    && e.CreatedAt < monthEnd, ct);
            if (granted) continue;

            up.AddMonthly(amount, key);

            var entry = new PointLedgerEntry
            {
                UserId = u.Id,
                Delta = amount,
                Reason = "GRANT_MONTHLY",
                RefType = "MonthlyGrant",
                RefId = null,
                IsMonthlyBucket = true,
                BalanceMonthlyAfter = up.MonthlyPoints,
                BalancePermanentAfter = up.PermanentPoints,
                Bucket = "MONTHLY",
                MonthKey = key,
                TxType = "GRANT_MONTHLY"
            };

            _db.PointLedgerEntries.Add(entry);
        }

        await _db.SaveChangesAsync(true, ct);
        return Result.Ok();
    }
}
