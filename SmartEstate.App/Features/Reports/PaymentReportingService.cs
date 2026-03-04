using Microsoft.EntityFrameworkCore;
using SmartEstate.App.Features.Reports.Dtos;
using SmartEstate.Domain.Enums;
using SmartEstate.Infrastructure.Persistence;
using SmartEstate.Shared.Results;

namespace SmartEstate.App.Features.Reports;

public sealed class PaymentReportingService
{
    private readonly SmartEstateDbContext _db;

    public PaymentReportingService(SmartEstateDbContext db)
    {
        _db = db;
    }

    public async Task<Result<PointPurchaseTotalsResponse>> GetPointPurchaseTotalsAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)
    {
        var payments = await _db.Payments
            .AsNoTracking()
            .Where(x => !x.IsDeleted
                && x.Status == PaymentStatus.Paid
                && x.RefType == RefType.PointTransaction
                && x.CreatedAt >= from
                && x.CreatedAt <= to)
            .ToListAsync(ct);

        var count = payments.Count;
        var currency = payments.FirstOrDefault()?.Currency ?? "VND";
        var total = payments.Sum(x => x.Amount);

        return Result<PointPurchaseTotalsResponse>.Ok(new PointPurchaseTotalsResponse(count, total, currency));
    }

    public async Task<Result<List<PaymentResponse>>> GetPaymentsAsync(CancellationToken ct = default)
    {
        var payments = await _db.Payments
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

        var userIds = payments.Select(x => x.PayerUserId).Distinct().ToList();
        var users = await _db.Users
            .AsNoTracking()
            .Where(x => userIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x, ct);

        return Result<List<PaymentResponse>>.Ok(payments.Select(x => {
            users.TryGetValue(x.PayerUserId, out var u);
            return new PaymentResponse(
                x.Id,
                x.PayerUserId,
                u?.DisplayName,
                u?.Email,
                x.Amount,
                x.Currency,
                x.FeeType.ToString(),
                x.RefType.ToString(),
                x.RefId,
                x.Status.ToString(),
                x.PaidAt,
                x.CreatedAt,
                x.Description
            );
        }).ToList());
    }
    
    public async Task<Result<AdminDashboardStats>> GetDashboardStatsAsync(CancellationToken ct = default)
    {
        var totalListings = await _db.Listings.CountAsync(x => !x.IsDeleted, ct);
        var totalUsers = await _db.Users.CountAsync(x => !x.IsDeleted, ct);
        var pendingMod = await _db.Listings.CountAsync(x => !x.IsDeleted && x.ModerationStatus == ModerationStatus.PendingReview, ct);
        var revenue = await _db.Payments
            .Where(x => !x.IsDeleted && x.Status == PaymentStatus.Paid)
            .SumAsync(x => x.Amount, ct);

        return Result<AdminDashboardStats>.Ok(new AdminDashboardStats(totalListings, totalUsers, pendingMod, revenue));
    }
}
