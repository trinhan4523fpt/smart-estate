using Microsoft.EntityFrameworkCore;
using SmartEstate.App.Common.Abstractions;
using SmartEstate.App.Features.BrokerTakeover.Dtos;
using SmartEstate.Domain.Entities;
using SmartEstate.Domain.Enums;
using SmartEstate.Infrastructure.Persistence;
using SmartEstate.Shared.Errors;
using SmartEstate.Shared.Results;
using SmartEstate.Shared.Time;
using SmartEstate.App.Features.Points;

namespace SmartEstate.App.Features.BrokerTakeover;

public sealed class TakeoverService
{
    private readonly SmartEstateDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly IPaymentGateway _payments;
    private readonly PointsService _points;

    public TakeoverService(SmartEstateDbContext db, ICurrentUser currentUser, IClock clock, IPaymentGateway payments, PointsService points)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _payments = payments;
        _points = points;
    }

    // Seller creates takeover request for a listing
    public async Task<Result<TakeoverResponse>> RequestAsync(RequestTakeoverRequest req, bool isAdmin, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Result<TakeoverResponse>.Fail(ErrorCodes.Unauthorized, "Unauthorized.");

        // listing must exist
        var listing = await _db.Listings.FirstOrDefaultAsync(x => x.Id == req.ListingId && !x.IsDeleted, ct);
        if (listing is null) return Result<TakeoverResponse>.Fail(ErrorCodes.NotFound, "Listing not found.");

        // only responsible seller/admin can request takeover
        if (!isAdmin && listing.ResponsibleUserId != userId.Value)
            return Result<TakeoverResponse>.Fail(ErrorCodes.Forbidden, "No permission to request takeover on this listing.");

        // broker must exist and be Broker role
        var broker = await _db.Users.Include(x => x.Role).FirstOrDefaultAsync(x => x.Id == req.BrokerUserId && !x.IsDeleted && x.IsActive, ct);
        if (broker is null) return Result<TakeoverResponse>.Fail(ErrorCodes.NotFound, "Broker user not found.");
        if (!string.Equals(broker.Role.Name, "Broker", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(broker.Role.Name, "Admin", StringComparison.OrdinalIgnoreCase))
            return Result<TakeoverResponse>.Fail(ErrorCodes.Validation, "Target user is not a broker.");

        // prevent duplicate active requests (pending/accepted) for same listing + broker
        var exists = await _db.TakeoverRequests.AnyAsync(x =>
            x.ListingId == req.ListingId &&
            x.BrokerUserId == req.BrokerUserId &&
            !x.IsDeleted &&
            (x.Status == TakeoverStatus.Pending || x.Status == TakeoverStatus.Accepted), ct);

        if (exists)
            return Result<TakeoverResponse>.Fail(ErrorCodes.Conflict, "A takeover request already exists.");

        // domain create
        var takeover = TakeoverRequest.Create(
            listingId: req.ListingId,
            sellerUserId: userId.Value,
            brokerUserId: req.BrokerUserId,
            payer: req.Payer,
            note: req.Note
        );

        // Pre-charge 30 points for takeover
        var spend = await _points.TrySpendAsync(
            takeover.SellerUserId,
            30,
            "SPEND_TAKEOVER",
            "TakeoverRequest",
            takeover.Id,
            ct);
        if (!spend.IsSuccess)
            return Result<TakeoverResponse>.Fail(ErrorCodes.Validation, "INSUFFICIENT_POINTS");
        takeover.MarkFeePaid(_clock.UtcNow);

        _db.TakeoverRequests.Add(takeover);
        await _db.SaveChangesAsync(true, ct);

        return Result<TakeoverResponse>.Ok(new TakeoverResponse(
            takeover.Id,
            takeover.ListingId,
            takeover.SellerUserId,
            takeover.BrokerUserId,
            takeover.Payer,
            takeover.IsFeePaid,
            takeover.PaidAt,
            takeover.Status
        ));
    }

    // Broker accepts/rejects. If accept => deduct points (-30) and complete takeover
    public async Task<Result<TakeoverResponse>> DecideAsync(Guid takeoverId, bool accept, bool isAdmin, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Result<TakeoverResponse>.Fail(ErrorCodes.Unauthorized, "Unauthorized.");

        var takeover = await _db.TakeoverRequests
            .FirstOrDefaultAsync(x => x.Id == takeoverId && !x.IsDeleted, ct);

        if (takeover is null) return Result<TakeoverResponse>.Fail(ErrorCodes.NotFound, "Takeover request not found.");

        // only broker (target) or admin can decide
        if (!isAdmin && takeover.BrokerUserId != userId.Value)
            return Result<TakeoverResponse>.Fail(ErrorCodes.Forbidden, "No permission to decide this takeover.");

        if (!accept)
        {
            takeover.Reject(_clock.UtcNow);
            // refund 30 points to seller
            await _points.AddPermanentAsync(takeover.SellerUserId, 30, "REFUND_TAKEOVER", "TakeoverRequest", takeover.Id, ct);
            await _db.SaveChangesAsync(true, ct);

            return Result<TakeoverResponse>.Ok(new TakeoverResponse(
                takeover.Id, takeover.ListingId, takeover.SellerUserId, takeover.BrokerUserId,
                takeover.Payer, takeover.IsFeePaid, takeover.PaidAt,
                takeover.Status
            ));
        }

        // accept => domain transition
        takeover.Accept(_clock.UtcNow);

        // complete takeover and assign broker
        takeover.Complete(_clock.UtcNow);

        var listing = await _db.Listings.FirstOrDefaultAsync(x => x.Id == takeover.ListingId && !x.IsDeleted, ct);
        if (listing is null) return Result<TakeoverResponse>.Fail(ErrorCodes.NotFound, "Listing not found.");
        listing.AssignBroker(takeover.BrokerUserId);

        // update conversations current responsible user
        var convs = await _db.Conversations.Where(x => x.ListingId == takeover.ListingId && !x.IsDeleted).ToListAsync(ct);
        foreach (var c in convs)
        {
            c.ResponsibleUserId = takeover.BrokerUserId;
        }

        await _db.SaveChangesAsync(true, ct);

        return Result<TakeoverResponse>.Ok(new TakeoverResponse(
            takeover.Id, takeover.ListingId, takeover.SellerUserId, takeover.BrokerUserId,
            takeover.Payer, takeover.IsFeePaid, takeover.PaidAt,
            takeover.Status
        ));
    }

    // Payment webhook path is no longer used for takeover fee in points-based flow

    // Seller unassign broker => take back responsibility
    public async Task<Result> UnassignBrokerAsync(Guid listingId, bool isAdmin, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Result.Fail(ErrorCodes.Unauthorized, "Unauthorized.");

        var listing = await _db.Listings.FirstOrDefaultAsync(x => x.Id == listingId && !x.IsDeleted, ct);
        if (listing is null) return Result.Fail(ErrorCodes.NotFound, "Listing not found.");

        if (!isAdmin && listing.CreatedByUserId != userId.Value)
            return Result.Fail(ErrorCodes.Forbidden, "Only seller can unassign broker.");

        listing.UnassignBroker();
        await _db.SaveChangesAsync(true, ct);
        return Result.Ok();
    }
}
