using Microsoft.EntityFrameworkCore;
using SmartEstate.App.Common.Abstractions;
using SmartEstate.App.Features.Listings.Dtos;
using SmartEstate.App.Features.Search.Dtos;
using SmartEstate.Domain.Entities;
using SmartEstate.Domain.Enums;
using SmartEstate.Infrastructure.Persistence;
using SmartEstate.Shared.Errors;
using SmartEstate.Shared.Paging;
using SmartEstate.Shared.Results;
using SmartEstate.Shared.Time;

namespace SmartEstate.App.Features.Listings;

public sealed class ListingService
{
    private readonly SmartEstateDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAiModerationService _moderation;
    private readonly IFileStorage _storage;

    public ListingService(
        SmartEstateDbContext db,
        ICurrentUser currentUser,
        IAiModerationService moderation,
        IFileStorage storage)
    {
        _db = db;
        _currentUser = currentUser;
        _moderation = moderation;
        _storage = storage;
    }

    public async Task<Result<ListingDetailResponse>> CreateAsync(CreateListingRequest req, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Result<ListingDetailResponse>.Fail(ErrorCodes.Unauthorized, "Unauthorized.");

        var update = ToUpdate(req);
        var listing = Listing.Create(userId.Value, update);

        if (req.Images != null)
        {
            int order = 0;
            foreach (var url in req.Images)
            {
                listing.Images.Add(new ListingImage 
                { 
                    ListingId = listing.Id, 
                    Url = url, 
                    SortOrder = order++ 
                });
            }
        }

        var mod = await _moderation.ModerateListingAsync(listing.Title, listing.Description, ct);
        listing.ApplyModerationDecision(mod.Decision, mod.QualityScore, mod.Reason, mod.FlagsJson);

        var report = ModerationReport.CreateFromAiDecision(
            listing.Id,
            mod.QualityScore,
            mod.Decision,
            mod.Reason,
            mod.FlagsJson);

        _db.Listings.Add(listing);
        _db.ModerationReports.Add(report);
        await _db.SaveChangesAsync(true, ct);

        return Result<ListingDetailResponse>.Ok(ToDetail(listing));
    }

    public async Task<Result<ListingDetailResponse>> UpdateAsync(Guid listingId, UpdateListingRequest req, bool isAdmin, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Result<ListingDetailResponse>.Fail(ErrorCodes.Unauthorized, "Unauthorized.");

        var listing = await _db.Listings
            .Include(x => x.Images.Where(i => !i.IsDeleted))
            .Include(x => x.ResponsibleUser)
            .FirstOrDefaultAsync(x => x.Id == listingId && !x.IsDeleted, ct);

        if (listing is null) return Result<ListingDetailResponse>.Fail(ErrorCodes.NotFound, "Listing not found.");

        if (!isAdmin && listing.ResponsibleUserId != userId.Value && listing.CreatedByUserId != userId.Value)
            return Result<ListingDetailResponse>.Fail(ErrorCodes.Forbidden, "No permission to update this listing.");

        var update = ToUpdate(req);
        listing.UpdateDetails(update);
        
        if (req.Images != null)
        {
             foreach(var existing in listing.Images) {
                 _db.ListingImages.Remove(existing);
             }
             
             int order = 0;
             foreach (var url in req.Images)
             {
                 listing.Images.Add(new ListingImage 
                 { 
                     ListingId = listing.Id, 
                     Url = url, 
                     SortOrder = order++ 
                 });
             }
        }

        var mod = await _moderation.ModerateListingAsync(listing.Title, listing.Description, ct);
        listing.ApplyModerationDecision(mod.Decision, mod.QualityScore, mod.Reason, mod.FlagsJson);

        var report = ModerationReport.CreateFromAiDecision(
            listing.Id,
            mod.QualityScore,
            mod.Decision,
            mod.Reason,
            mod.FlagsJson);

        _db.ModerationReports.Add(report);
        await _db.SaveChangesAsync(true, ct);

        return Result<ListingDetailResponse>.Ok(ToDetail(listing));
    }

    public async Task<Result<ListingDetailResponse>> GetDetailAsync(Guid listingId, Guid? viewerUserId, bool isAdmin, CancellationToken ct = default)
    {
        var query = _db.Listings
            .Include(x => x.Images.Where(i => !i.IsDeleted).OrderBy(i => i.SortOrder))
            .Include(x => x.ResponsibleUser)
            .AsQueryable();

        // If admin or owner, include more details
        // Note: checking permission after fetching is simpler, but we can do pre-check if needed.
        // For simplicity, we fetch and then check.
        
        var listing = await query.FirstOrDefaultAsync(x => x.Id == listingId && !x.IsDeleted, ct);

        if (listing is null) return Result<ListingDetailResponse>.Fail(ErrorCodes.NotFound, "Listing not found.");

        var isOwner = viewerUserId is not null && (listing.ResponsibleUserId == viewerUserId.Value || listing.CreatedByUserId == viewerUserId.Value);

        if (!isAdmin && !isOwner)
        {
            if (listing.ModerationStatus != ModerationStatus.Approved || listing.LifecycleStatus != ListingLifecycleStatus.Active)
                return Result<ListingDetailResponse>.Fail(ErrorCodes.Forbidden, "Listing is not public.");
        }

        object? brokerRequests = null;
        object? reports = null;

        if (isAdmin || isOwner)
        {
            // Load broker requests
            var brs = await _db.BrokerRequests
                .Where(x => x.ListingId == listingId && !x.IsDeleted)
                .Select(x => new 
                {
                    x.Id,
                    x.ListingId,
                    x.SellerId,
                    // SellerName?
                    x.BrokerId,
                    // BrokerName?
                    Status = x.Status.ToString().ToLower(),
                    x.TakeoverFeeAmount,
                    x.FeePaidByUserId,
                    FeeStatus = x.FeeStatus.ToString().ToLower(),
                    RequestedAt = x.CreatedAt,
                    x.RespondedAt,
                    x.PaidAt
                })
                .ToListAsync(ct);
            brokerRequests = brs;
        }

        if (isAdmin)
        {
            // Load reports
            var rpts = await _db.ListingReports
                .Where(x => x.ListingId == listingId && !x.IsDeleted)
                .Select(x => new
                {
                    x.Id,
                    x.ListingId,
                    UserId = x.CreatedBy, // The reporter
                    ReporterUserId = x.CreatedBy,
                    x.Reason,
                    Note = x.Detail,
                    ReportedAt = x.CreatedAt
                })
                .ToListAsync(ct);
            reports = rpts;
        }

        return Result<ListingDetailResponse>.Ok(ToDetail(listing, brokerRequests, reports));
    }
    
    public async Task<Result> UpdateLifecycleAsync(Guid listingId, ListingLifecycleStatus status, bool isAdmin, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Result.Fail(ErrorCodes.Unauthorized, "Unauthorized.");

        var listing = await _db.Listings.FirstOrDefaultAsync(x => x.Id == listingId && !x.IsDeleted, ct);
        if (listing is null) return Result.Fail(ErrorCodes.NotFound, "Listing not found.");

        if (!isAdmin && listing.ResponsibleUserId != userId.Value && listing.CreatedByUserId != userId.Value)
            return Result.Fail(ErrorCodes.Forbidden, "No permission to update lifecycle.");

        listing.SetLifecycle(status);

        await _db.SaveChangesAsync(true, ct);
        return Result.Ok();
    }
    
    public async Task<Result> SubmitAsync(Guid listingId, bool isAdmin, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Result.Fail(ErrorCodes.Unauthorized, "Unauthorized.");

        var listing = await _db.Listings.FirstOrDefaultAsync(x => x.Id == listingId && !x.IsDeleted, ct);
        if (listing is null) return Result.Fail(ErrorCodes.NotFound, "Listing not found.");

        if (!isAdmin && listing.ResponsibleUserId != userId.Value && listing.CreatedByUserId != userId.Value)
            return Result.Fail(ErrorCodes.Forbidden, "No permission.");

        listing.NeedReview("Submitted by user.");
        await _db.SaveChangesAsync(true, ct);
        return Result.Ok();
    }

    public async Task<Result> DeleteAsync(Guid listingId, bool isAdmin, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Result.Fail(ErrorCodes.Unauthorized, "Unauthorized.");

        var listing = await _db.Listings.FirstOrDefaultAsync(x => x.Id == listingId && !x.IsDeleted, ct);
        if (listing is null) return Result.Fail(ErrorCodes.NotFound, "Listing not found.");

        if (!isAdmin && listing.CreatedByUserId != userId.Value)
            return Result.Fail(ErrorCodes.Forbidden, "Only owner can delete listing.");

        _db.Listings.Remove(listing);
        await _db.SaveChangesAsync(true, ct);
        return Result.Ok();
    }

    public async Task<Result<string>> GetContactAsync(Guid listingId, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Result<string>.Fail(ErrorCodes.Unauthorized, "Unauthorized.");

        var listing = await _db.Listings
            .Include(x => x.ResponsibleUser)
            .FirstOrDefaultAsync(x => x.Id == listingId && !x.IsDeleted, ct);

        if (listing is null) return Result<string>.Fail(ErrorCodes.NotFound, "Listing not found.");

        return Result<string>.Ok(listing.ResponsibleUser.Phone ?? listing.SellerPhone ?? "");
    }

    public async Task<Result<PagedResult<ListingDetailResponse>>> SearchAsync(SearchRequest req, CancellationToken ct = default)
    {
        var query = _db.Listings
            .AsNoTracking()
            .Include(x => x.Images.Where(i => !i.IsDeleted))
            .Include(x => x.ResponsibleUser)
            .Where(x => !x.IsDeleted 
                        && x.ModerationStatus == ModerationStatus.Approved 
                        && x.LifecycleStatus == ListingLifecycleStatus.Active);

        if (!string.IsNullOrWhiteSpace(req.Keyword))
        {
            var k = req.Keyword.Trim().ToLower();
            query = query.Where(x => x.Title.ToLower().Contains(k) || x.Description.ToLower().Contains(k));
        }
        
        if (!string.IsNullOrWhiteSpace(req.City)) query = query.Where(x => x.City == req.City);
        if (!string.IsNullOrWhiteSpace(req.District)) query = query.Where(x => x.District == req.District);
        
        if (req.PropertyType.HasValue) query = query.Where(x => x.PropertyType == req.PropertyType);
        if (req.TransactionType.HasValue) query = query.Where(x => x.TransactionType == req.TransactionType);
        
        if (req.MinPrice.HasValue) query = query.Where(x => x.Price >= req.MinPrice);
        if (req.MaxPrice.HasValue) query = query.Where(x => x.Price <= req.MaxPrice);
        
        if (req.MinAreaM2.HasValue) query = query.Where(x => x.AreaM2 >= req.MinAreaM2);
        if (req.MinBedrooms.HasValue) query = query.Where(x => x.Bedrooms >= req.MinBedrooms);

        // Sorting
        query = req.Sort switch
        {
            "price_asc" => query.OrderBy(x => x.Price),
            "price_desc" => query.OrderByDescending(x => x.Price),
            _ => query.OrderByDescending(x => x.CreatedAt)
        };

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .ToListAsync(ct);

        var dtos = items.Select(x => ToDetail(x)).ToList();
        return Result<PagedResult<ListingDetailResponse>>.Ok(new PagedResult<ListingDetailResponse>(dtos, total, req.Page, req.PageSize));
    }

    public async Task<Result<List<ListingDetailResponse>>> GetMyListingsAsync(CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Result<List<ListingDetailResponse>>.Fail(ErrorCodes.Unauthorized, "Unauthorized.");

        var listings = await _db.Listings
            .AsNoTracking()
            .Include(x => x.Images.Where(i => !i.IsDeleted))
            .Include(x => x.ResponsibleUser)
            .Where(x => x.CreatedByUserId == userId.Value && !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

        return Result<List<ListingDetailResponse>>.Ok(listings.Select(x => ToDetail(x)).ToList());
    }

    public async Task<Result<List<ListingDetailResponse>>> GetAdminListingsAsync(CancellationToken ct = default)
    {
        // Admin sees all listings, including deleted? Maybe just not deleted for now.
        // Frontend admin dashboard likely wants to see everything or filter.
        // `fetchAdminListings` in frontend just calls `/api/admin/listings`.
        
        var listings = await _db.Listings
            .AsNoTracking()
            .Include(x => x.Images.Where(i => !i.IsDeleted))
            .Include(x => x.ResponsibleUser)
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

        return Result<List<ListingDetailResponse>>.Ok(listings.Select(x => ToDetail(x)).ToList());
    }

    public async Task<Result> ReportAsync(Guid listingId, string reason, string? note, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Result.Fail(ErrorCodes.Unauthorized, "Unauthorized.");

        var exists = await _db.Listings.AnyAsync(x => x.Id == listingId && !x.IsDeleted, ct);
        if (!exists) return Result.Fail(ErrorCodes.NotFound, "Listing not found.");

        var report = new ListingReport
        {
            ListingId = listingId,
            ReporterUserId = userId.Value,
            Reason = reason,
            Detail = note
        };

        _db.ListingReports.Add(report);
        await _db.SaveChangesAsync(true, ct);
        return Result.Ok();
    }

    public async Task<Result<PagedResult<ListingDetailResponse>>> GetMyFavoritesAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Result<PagedResult<ListingDetailResponse>>.Fail(ErrorCodes.Unauthorized, "Unauthorized.");

        var query = _db.UserListingFavorites
            .AsNoTracking()
            .Where(x => x.UserId == userId.Value)
            .Select(x => x.Listing)
            .Where(x => !x.IsDeleted)
            .Include(x => x.Images)
            .Include(x => x.ResponsibleUser);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
            
        var dtos = items.Select(x => ToDetail(x)).ToList();
        return Result<PagedResult<ListingDetailResponse>>.Ok(new PagedResult<ListingDetailResponse>(dtos, total, page, pageSize));
    }

    public async Task<Result> AddFavoriteAsync(Guid listingId, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Result.Fail(ErrorCodes.Unauthorized, "Unauthorized.");

        var exists = await _db.Listings.AnyAsync(x => x.Id == listingId && !x.IsDeleted, ct);
        if (!exists) return Result.Fail(ErrorCodes.NotFound, "Listing not found.");

        var fav = await _db.UserListingFavorites.FirstOrDefaultAsync(x => x.UserId == userId.Value && x.ListingId == listingId, ct);
        if (fav != null) return Result.Ok();

        _db.UserListingFavorites.Add(new UserListingFavorite { UserId = userId.Value, ListingId = listingId });
        await _db.SaveChangesAsync(true, ct);
        return Result.Ok();
    }

    public async Task<Result> RemoveFavoriteAsync(Guid listingId, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Result.Fail(ErrorCodes.Unauthorized, "Unauthorized.");

        var fav = await _db.UserListingFavorites.FirstOrDefaultAsync(x => x.UserId == userId.Value && x.ListingId == listingId, ct);
        if (fav == null) return Result.Ok();

        _db.UserListingFavorites.Remove(fav);
        await _db.SaveChangesAsync(true, ct);
        return Result.Ok();
    }

    private static ListingUpdate ToUpdate(CreateListingRequest r) => new(
        Title: r.Title,
        Description: r.Description,
        PropertyType: r.PropertyType,
        TransactionType: r.TransactionType,
        Price: r.Price,
        AreaM2: r.AreaM2,
        Bedrooms: r.Bedrooms,
        Bathrooms: r.Bathrooms,
        City: r.City,
        District: r.District,
        Address: r.Address,
        Lat: r.Lat,
        Lng: r.Lng,
        VirtualTourUrl: r.VirtualTourUrl,
        SellerName: null,
        SellerPhone: null
    );

    private static ListingUpdate ToUpdate(UpdateListingRequest r) => new(
        Title: r.Title,
        Description: r.Description,
        PropertyType: r.PropertyType,
        TransactionType: r.TransactionType,
        Price: r.Price,
        AreaM2: r.AreaM2,
        Bedrooms: r.Bedrooms,
        Bathrooms: r.Bathrooms,
        City: r.City,
        District: r.District,
        Address: r.Address,
        Lat: r.Lat,
        Lng: r.Lng,
        VirtualTourUrl: r.VirtualTourUrl,
        SellerName: null,
        SellerPhone: null
    );

    private static ListingDetailResponse ToDetail(Listing l, object? brokerRequests = null, object? reports = null)
    {
        return new ListingDetailResponse(
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
            brokerRequests,
            reports
        );
    }
}
