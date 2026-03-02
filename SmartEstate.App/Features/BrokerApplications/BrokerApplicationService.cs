using Microsoft.EntityFrameworkCore;
using SmartEstate.App.Features.BrokerApplications.Dtos;
using SmartEstate.App.Features.Points;
using SmartEstate.Domain.Entities;
using SmartEstate.Domain.Enums;
using SmartEstate.Infrastructure.Persistence;
using SmartEstate.Shared.Errors;
using SmartEstate.Shared.Results;
using SmartEstate.Shared.Time;
using SmartEstate.App.Common.Abstractions;

namespace SmartEstate.App.Features.BrokerApplications;

public sealed class BrokerApplicationService
{
    private readonly SmartEstateDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly PointsService _points;

    public BrokerApplicationService(SmartEstateDbContext db, ICurrentUser currentUser, IClock clock, PointsService points)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _points = points;
    }

    public async Task<Result<BrokerApplicationResponse>> CreateAsync(CreateBrokerApplicationRequest req, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Result<BrokerApplicationResponse>.Fail(ErrorCodes.Unauthorized, "Unauthorized.");

        var user = await _db.Users.Include(x => x.Role).FirstOrDefaultAsync(x => x.Id == userId.Value && !x.IsDeleted && x.IsActive, ct);
        if (user is null) return Result<BrokerApplicationResponse>.Fail(ErrorCodes.NotFound, "User not found.");

        if (string.Equals(user.Role.Name, "Broker", StringComparison.OrdinalIgnoreCase)
            || string.Equals(user.Role.Name, "Admin", StringComparison.OrdinalIgnoreCase))
            return Result<BrokerApplicationResponse>.Fail(ErrorCodes.Validation, "User already broker/admin.");

        var hasPending = await _db.BrokerApplications.AnyAsync(x => x.UserId == userId.Value && !x.IsDeleted && x.Status == BrokerApplicationStatus.Pending, ct);
        if (hasPending)
            return Result<BrokerApplicationResponse>.Fail(ErrorCodes.Conflict, "Pending application exists.");

        var app = new BrokerApplication
        {
            UserId = userId.Value,
            DocUrl = string.IsNullOrWhiteSpace(req.DocUrl) ? null : req.DocUrl.Trim(),
            Status = BrokerApplicationStatus.Pending,
            IsActivationPaid = false
        };

        _db.BrokerApplications.Add(app);
        await _db.SaveChangesAsync(true, ct);

        return Result<BrokerApplicationResponse>.Ok(ToResponse(app));
    }

    public async Task<Result> AdminApproveAsync(Guid applicationId, CancellationToken ct = default)
    {
        var adminId = _currentUser.UserId;
        if (adminId is null) return Result.Fail(ErrorCodes.Unauthorized, "Unauthorized.");

        var admin = await _db.Users.Include(x => x.Role).FirstOrDefaultAsync(x => x.Id == adminId.Value && !x.IsDeleted && x.IsActive, ct);
        if (admin is null || !string.Equals(admin.Role.Name, "Admin", StringComparison.OrdinalIgnoreCase)) return Result.Fail(ErrorCodes.Forbidden, "Admin only.");

        var app = await _db.BrokerApplications.FirstOrDefaultAsync(x => x.Id == applicationId && !x.IsDeleted, ct);
        if (app is null) return Result.Fail(ErrorCodes.NotFound, "Application not found.");

        var user = await _db.Users.Include(x => x.Role).FirstOrDefaultAsync(x => x.Id == app.UserId && !x.IsDeleted && x.IsActive, ct);
        if (user is null) return Result.Fail(ErrorCodes.NotFound, "User not found.");

        if (app.Status == BrokerApplicationStatus.Approved) return Result.Ok();
        if (app.Status == BrokerApplicationStatus.Rejected) return Result.Fail(ErrorCodes.Validation, "Application already rejected.");

        // deduct -60 points (no negative)
        var r = await _points.TrySpendAsync(
            app.UserId,
            60,
            "SPEND_BROKER_ACTIVATION",
            "BrokerApplication",
            app.Id,
            ct);

        if (!r.IsSuccess) return r; // INSUFFICIENT_POINTS

        app.Status = BrokerApplicationStatus.Approved;
        app.ReviewedByAdminId = adminId.Value;
        app.ReviewedAt = _clock.UtcNow;
        app.IsActivationPaid = true;
        app.ActivationPaidAt = _clock.UtcNow;

        var brokerRole = await _db.Roles.AsNoTracking().FirstOrDefaultAsync(x => x.Name == "Broker", ct);
        if (brokerRole is not null)
            user.SetRoleId(brokerRole.Id);

        await _db.SaveChangesAsync(true, ct);
        return Result.Ok();
    }

    public async Task<Result> AdminRejectAsync(Guid applicationId, CancellationToken ct = default)
    {
        var adminId = _currentUser.UserId;
        if (adminId is null) return Result.Fail(ErrorCodes.Unauthorized, "Unauthorized.");

        var admin = await _db.Users.Include(x => x.Role).FirstOrDefaultAsync(x => x.Id == adminId.Value && !x.IsDeleted && x.IsActive, ct);
        if (admin is null || !string.Equals(admin.Role.Name, "Admin", StringComparison.OrdinalIgnoreCase)) return Result.Fail(ErrorCodes.Forbidden, "Admin only.");

        var app = await _db.BrokerApplications.FirstOrDefaultAsync(x => x.Id == applicationId && !x.IsDeleted, ct);
        if (app is null) return Result.Fail(ErrorCodes.NotFound, "Application not found.");

        if (app.Status == BrokerApplicationStatus.Rejected) return Result.Ok();
        if (app.Status == BrokerApplicationStatus.Approved) return Result.Fail(ErrorCodes.Validation, "Application already approved.");

        app.Status = BrokerApplicationStatus.Rejected;
        app.ReviewedByAdminId = adminId.Value;
        app.ReviewedAt = _clock.UtcNow;

        await _db.SaveChangesAsync(true, ct);
        return Result.Ok();
    }

    private static BrokerApplicationResponse ToResponse(BrokerApplication app)
        => new BrokerApplicationResponse(app.Id, app.UserId, app.DocUrl, (int)app.Status, app.IsActivationPaid, app.ActivationPaidAt, app.ReviewedByAdminId, app.ReviewedAt);
}

