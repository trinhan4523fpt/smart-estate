using Microsoft.EntityFrameworkCore;
using SmartEstate.App.Common.Abstractions;
using SmartEstate.App.Features.BrokerRequests.Dtos;
using SmartEstate.App.Features.Listings.Dtos;
using SmartEstate.App.Features.Listings;
using SmartEstate.Domain.Entities;
using SmartEstate.Domain.Enums;
using SmartEstate.Infrastructure.Persistence;
using SmartEstate.Shared.Errors;
using SmartEstate.Shared.Results;
using SmartEstate.Shared.Time;

namespace SmartEstate.App.Features.BrokerRequests;

public sealed class BrokerRequestService
{
    private readonly SmartEstateDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public BrokerRequestService(SmartEstateDbContext db, ICurrentUser currentUser, IClock clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result<BrokerRequestResponse>> CreateAsync(Guid listingId, Guid brokerId, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Result<BrokerRequestResponse>.Fail(ErrorCodes.Unauthorized, "Unauthorized.");

        var listing = await _db.Listings.FirstOrDefaultAsync(x => x.Id == listingId && !x.IsDeleted, ct);
        if (listing is null) return Result<BrokerRequestResponse>.Fail(ErrorCodes.NotFound, "Listing not found.");

        if (listing.ResponsibleUserId != userId.Value)
            return Result<BrokerRequestResponse>.Fail(ErrorCodes.Forbidden, "Only seller can request takeover.");

        var broker = await _db.Users.FirstOrDefaultAsync(x => x.Id == brokerId && !x.IsDeleted && x.IsActive, ct);
        if (broker is null || broker.Role != UserRole.Broker)
            return Result<BrokerRequestResponse>.Fail(ErrorCodes.Validation, "Target user is not a broker.");

        var exists = await _db.BrokerRequests.AnyAsync(x =>
            x.ListingId == listingId &&
            x.BrokerId == brokerId &&
            !x.IsDeleted &&
            (x.Status == TakeoverStatus.Pending || x.Status == TakeoverStatus.Accepted), ct);

        if (exists)
            return Result<BrokerRequestResponse>.Fail(ErrorCodes.Conflict, "A takeover request already exists.");

        var request = BrokerRequest.Create(listingId, userId.Value, brokerId, 500000); // Default fee

        _db.BrokerRequests.Add(request);
        await _db.SaveChangesAsync(true, ct);

        return Result<BrokerRequestResponse>.Ok(ToResponse(request, null, null));
    }

    public async Task<Result<List<BrokerRequestResponse>>> GetMyRequestsAsync(CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Result<List<BrokerRequestResponse>>.Fail(ErrorCodes.Unauthorized, "Unauthorized.");

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId.Value, ct);
        if (user is null) return Result<List<BrokerRequestResponse>>.Fail(ErrorCodes.Unauthorized, "User not found.");

        IQueryable<BrokerRequest> q;
        
        if (user.Role == UserRole.Broker)
        {
            q = _db.BrokerRequests
                .AsNoTracking()
                .Where(x => x.BrokerId == userId.Value && !x.IsDeleted);
        }
        else
        {
            q = _db.BrokerRequests
                .AsNoTracking()
                .Where(x => x.SellerId == userId.Value && !x.IsDeleted);
        }

        var requests = await q
            .Include(x => x.Seller)
            .Include(x => x.Broker)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

        return Result<List<BrokerRequestResponse>>.Ok(requests.Select(x => ToResponse(x, x.Seller.DisplayName, x.Broker.DisplayName)).ToList());
    }

    public async Task<Result<List<BrokerRequestResponse>>> GetSentRequestsAsync(CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Result<List<BrokerRequestResponse>>.Fail(ErrorCodes.Unauthorized, "Unauthorized.");

        var requests = await _db.BrokerRequests
            .AsNoTracking()
            .Include(x => x.Seller)
            .Include(x => x.Broker)
            .Where(x => x.SellerId == userId.Value && !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

        return Result<List<BrokerRequestResponse>>.Ok(requests.Select(x => ToResponse(x, x.Seller.DisplayName, x.Broker.DisplayName)).ToList());
    }

    public async Task<Result<List<BrokerRequestResponse>>> GetReceivedRequestsAsync(CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Result<List<BrokerRequestResponse>>.Fail(ErrorCodes.Unauthorized, "Unauthorized.");

        var requests = await _db.BrokerRequests
            .AsNoTracking()
            .Include(x => x.Seller)
            .Include(x => x.Broker)
            .Where(x => x.BrokerId == userId.Value && !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

        return Result<List<BrokerRequestResponse>>.Ok(requests.Select(x => ToResponse(x, x.Seller.DisplayName, x.Broker.DisplayName)).ToList());
    }

    public async Task<Result<BrokerRequestResponse>> RespondAsync(Guid requestId, string status, CancellationToken ct = default)
    {
        if (status.ToLower() == "accepted") return await AcceptAsync(requestId, ct);
        if (status.ToLower() == "rejected") return await RejectAsync(requestId, ct);
        return Result<BrokerRequestResponse>.Fail(ErrorCodes.Validation, "Invalid status. Use 'accepted' or 'rejected'.");
    }

    public async Task<Result<BrokerRequestResponse>> AcceptAsync(Guid requestId, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Result<BrokerRequestResponse>.Fail(ErrorCodes.Unauthorized, "Unauthorized.");

        var request = await _db.BrokerRequests.FirstOrDefaultAsync(x => x.Id == requestId && !x.IsDeleted, ct);
        if (request is null) return Result<BrokerRequestResponse>.Fail(ErrorCodes.NotFound, "Request not found.");

        if (request.BrokerId != userId.Value)
            return Result<BrokerRequestResponse>.Fail(ErrorCodes.Forbidden, "Only assigned broker can accept.");

        if (request.Status != TakeoverStatus.Pending)
            return Result<BrokerRequestResponse>.Fail(ErrorCodes.Validation, "Request is not pending.");

        request.Accept(_clock.UtcNow);
        await _db.SaveChangesAsync(true, ct);

        return Result<BrokerRequestResponse>.Ok(ToResponse(request, null, null));
    }

    public async Task<Result<BrokerRequestResponse>> RejectAsync(Guid requestId, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Result<BrokerRequestResponse>.Fail(ErrorCodes.Unauthorized, "Unauthorized.");

        var request = await _db.BrokerRequests.FirstOrDefaultAsync(x => x.Id == requestId && !x.IsDeleted, ct);
        if (request is null) return Result<BrokerRequestResponse>.Fail(ErrorCodes.NotFound, "Request not found.");

        if (request.BrokerId != userId.Value)
            return Result<BrokerRequestResponse>.Fail(ErrorCodes.Forbidden, "Only assigned broker can reject.");

        if (request.Status != TakeoverStatus.Pending)
            return Result<BrokerRequestResponse>.Fail(ErrorCodes.Validation, "Request is not pending.");

        request.Reject(_clock.UtcNow);
        await _db.SaveChangesAsync(true, ct);

        return Result<BrokerRequestResponse>.Ok(ToResponse(request, null, null));
    }

    public async Task<Result> DeleteAsync(Guid requestId, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Result.Fail(ErrorCodes.Unauthorized, "Unauthorized.");

        var request = await _db.BrokerRequests.FirstOrDefaultAsync(x => x.Id == requestId && !x.IsDeleted, ct);
        if (request is null) return Result.Fail(ErrorCodes.NotFound, "Request not found.");

        // Allow deletion if pending and owner is seller? Or if rejected?
        // Frontend uses this for unassigning broker (which might be active?)
        // If status is Accepted/Paid, unassign means remove broker from listing?
        // Logic: if request is related to a listing, and we want to unassign, we might need to update listing.
        // But if just deleting request, soft delete.
        
        // Let's assume unassign logic:
        if (request.Status == TakeoverStatus.Accepted && request.FeeStatus == FeeStatus.Paid)
        {
             var listing = await _db.Listings.FirstOrDefaultAsync(x => x.Id == request.ListingId, ct);
             if (listing != null && listing.AssignedBrokerUserId == request.BrokerId)
             {
                 listing.UnassignBroker();
             }
        }

        request.IsDeleted = true;
        request.DeletedAt = _clock.UtcNow;
        request.DeletedBy = userId.Value;
        
        await _db.SaveChangesAsync(true, ct);
        return Result.Ok();
    }

    public async Task<Result<BrokerRequestResponse>> PayFeeAsync(Guid requestId, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Result<BrokerRequestResponse>.Fail(ErrorCodes.Unauthorized, "Unauthorized.");

        var request = await _db.BrokerRequests.FirstOrDefaultAsync(x => x.Id == requestId && !x.IsDeleted, ct);
        if (request is null) return Result<BrokerRequestResponse>.Fail(ErrorCodes.NotFound, "Request not found.");

        if (request.SellerId != userId.Value)
            return Result<BrokerRequestResponse>.Fail(ErrorCodes.Forbidden, "Only seller can pay fee.");

        if (request.Status != TakeoverStatus.Accepted)
            return Result<BrokerRequestResponse>.Fail(ErrorCodes.Validation, "Request must be accepted first.");

        if (request.FeeStatus == FeeStatus.Paid)
            return Result<BrokerRequestResponse>.Fail(ErrorCodes.Validation, "Already paid.");

        // Simulate payment
        request.ConfirmPayment(userId.Value, _clock.UtcNow);

        // Assign broker to listing
        var listing = await _db.Listings.FirstOrDefaultAsync(x => x.Id == request.ListingId, ct);
        if (listing != null)
        {
            listing.AssignBroker(request.BrokerId);
        }

        await _db.SaveChangesAsync(true, ct);

        return Result<BrokerRequestResponse>.Ok(ToResponse(request, null, null));
    }

    public async Task<Result<List<ListingDetailResponse>>> GetManagedListingsAsync(CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Result<List<ListingDetailResponse>>.Fail(ErrorCodes.Unauthorized, "Unauthorized.");

        var listings = await _db.Listings
            .AsNoTracking()
            .Include(x => x.Images.Where(i => !i.IsDeleted))
            .Include(x => x.ResponsibleUser)
            .Where(x => x.AssignedBrokerUserId == userId.Value && !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

        // Need to duplicate logic from ListingService.ToDetail roughly, or just simple map.
        // Ideally reuse ListingService but circular dependency.
        // We'll duplicate simple mapping.
        
        return Result<List<ListingDetailResponse>>.Ok(listings.Select(l => new ListingDetailResponse(
            l.Id,
            l.Title,
            l.Description,
            l.PropertyType,
            l.TransactionType,
            l.Price,
            l.AreaM2,
            l.Bedrooms,
            l.Bathrooms,
            l.City,
            l.District,
            l.Address,
            l.Lat,
            l.Lng,
            l.VirtualTourUrl,
            l.SellerName,
            l.SellerPhone,
            l.IsBrokerManaged,
            l.ModerationStatus,
            l.ModerationReason,
            l.LifecycleStatus,
            l.CreatedByUserId,
            l.ResponsibleUserId,
            l.Images.Select(x => x.Url).ToList(),
            l.CreatedAt,
            l.UpdatedAt,
            l.ApprovedAt,
            null,
            null
        )).ToList());
    }

    private static BrokerRequestResponse ToResponse(BrokerRequest r, string? sellerName, string? brokerName) => new(
        r.Id,
        r.ListingId,
        r.SellerId,
        sellerName,
        r.BrokerId,
        brokerName,
        r.Status.ToString().ToLowerInvariant(),
        r.TakeoverFeeAmount,
        r.FeePaidByUserId,
        r.FeeStatus.ToString().ToLowerInvariant(),
        r.CreatedAt,
        r.RespondedAt,
        r.PaidAt
    );
}
