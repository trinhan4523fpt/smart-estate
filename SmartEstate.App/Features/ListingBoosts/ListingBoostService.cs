using Microsoft.EntityFrameworkCore;
using SmartEstate.App.Common.Abstractions;
using SmartEstate.App.Features.Points;
using SmartEstate.Domain.Entities;
using SmartEstate.Infrastructure.Persistence;
using SmartEstate.Shared.Errors;
using SmartEstate.Shared.Results;
using SmartEstate.Shared.Time;

namespace SmartEstate.App.Features.ListingBoosts;

public sealed class ListingBoostService
{
    private readonly SmartEstateDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly PointsService _points;

    public ListingBoostService(SmartEstateDbContext db, ICurrentUser currentUser, IClock clock, PointsService points)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _points = points;
    }

    public async Task<Result> BoostAsync(Guid listingId, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Result.Fail(ErrorCodes.Unauthorized, "Unauthorized.");

        var listing = await _db.Listings.FirstOrDefaultAsync(x => x.Id == listingId && !x.IsDeleted, ct);
        if (listing is null) return Result.Fail(ErrorCodes.NotFound, "Listing not found.");

        var isAdmin = false; // controller decides role; here assume check by responsible
        if (!isAdmin && listing.ResponsibleUserId != userId.Value)
            return Result.Fail(ErrorCodes.Forbidden, "No permission to boost this listing.");

        var now = _clock.UtcNow;

        var hasActive = await _db.ListingBoosts.AnyAsync(x =>
            x.ListingId == listingId && !x.IsDeleted && x.StartsAt <= now && x.EndsAt > now, ct);

        if (hasActive) return Result.Fail(ErrorCodes.Conflict, "ACTIVE_BOOST_EXISTS");

        var boost = new ListingBoost
        {
            ListingId = listingId,
            UserId = userId.Value,
            StartsAt = now,
            EndsAt = now.AddDays(7)
        };

        _db.ListingBoosts.Add(boost);
        await _db.SaveChangesAsync(true, ct);

        var spend = await _points.TrySpendAsync(userId.Value, 10, "SPEND_BOOST", "ListingBoost", boost.Id, ct);
        if (!spend.IsSuccess)
        {
            // rollback if insufficient points
            boost.IsDeleted = true;
            boost.DeletedAt = now;
            boost.DeletedBy = userId.Value;
            await _db.SaveChangesAsync(true, ct);
            return spend;
        }

        return Result.Ok();
    }
}

