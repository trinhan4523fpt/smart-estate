using Microsoft.EntityFrameworkCore;
using SmartEstate.App.Common.Abstractions;
using SmartEstate.App.Features.Listings.Dtos;
using SmartEstate.App.Features.Points;
using SmartEstate.Domain.Entities;
using SmartEstate.Domain.Enums;
using SmartEstate.Infrastructure.Persistence;
using SmartEstate.Shared.Errors;
using SmartEstate.Shared.Results;
using SmartEstate.Shared.Time;

namespace SmartEstate.App.Features.Listings;

public sealed class ListingService
{
    private readonly SmartEstateDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAiModerationService _moderation;
    private readonly IFileStorage _storage;
    private readonly PointsService _points;

    public ListingService(
        SmartEstateDbContext db,
        ICurrentUser currentUser,
        IAiModerationService moderation,
        IFileStorage storage,
        PointsService points)
    {
        _db = db;
        _currentUser = currentUser;
        _moderation = moderation;
        _storage = storage;
        _points = points;
    }

    public async Task<Result<ListingDetailResponse>> CreateAsync(CreateListingRequest req, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Result<ListingDetailResponse>.Fail(ErrorCodes.Unauthorized, "Unauthorized.");

        var update = ToUpdate(req);

        var listing = Listing.Create(userId.Value, update);
        // Trừ điểm trước khi nộp bài
        var spend = await _points.TrySpendAsync(listing.CreatedByUserId, 1, "SPEND_POST", "Listing", listing.Id, ct);
        if (!spend.IsSuccess)
            return Result<ListingDetailResponse>.Fail(ErrorCodes.Validation, "INSUFFICIENT_POINTS");

        var mod = await _moderation.ModerateListingAsync(listing.Title, listing.Description, ct);
        listing.ApplyModerationDecision(mod.Decision, mod.QualityScore, mod.Reason, mod.FlagsJson);

        var report = ModerationReport.CreateFromAiDecision(
            listing.Id,
            mod.QualityScore,
            mod.Decision,
            mod.Reason,
            mod.FlagsJson);

<<<<<<< Updated upstream
        if (mod.Decision == "AUTO_REJECT")
            await _points.AddPermanentAsync(listing.CreatedByUserId, 1, "REFUND_POST", "Listing", listing.Id, ct);

=======
>>>>>>> Stashed changes
        _db.Listings.Add(listing);
        _db.ModerationReports.Add(report);
        await _db.SaveChangesAsync(true, ct);

        return Result<ListingDetailResponse>.Ok(ToDetail(listing, new List<ListingImageDto>()));
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

        if (!isAdmin && listing.ResponsibleUserId != userId.Value)
            return Result<ListingDetailResponse>.Fail(ErrorCodes.Forbidden, "No permission to update this listing.");

        var update = ToUpdate(req);

        listing.UpdateDetails(update);

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

        var imgs = listing.Images
            .OrderBy(i => i.SortOrder)
            .Select(i => new ListingImageDto(i.Id, i.Url, i.SortOrder, i.Caption))
            .ToList();

        return Result<ListingDetailResponse>.Ok(ToDetail(listing, imgs));
    }

    public async Task<Result<ListingDetailResponse>> GetDetailAsync(Guid listingId, Guid? viewerUserId, bool isAdmin, CancellationToken ct = default)
    {
        var listing = await _db.Listings
            .Include(x => x.Images.Where(i => !i.IsDeleted))
            .FirstOrDefaultAsync(x => x.Id == listingId && !x.IsDeleted, ct);

        if (listing is null) return Result<ListingDetailResponse>.Fail(ErrorCodes.NotFound, "Listing not found.");

        var isOwner = viewerUserId is not null && listing.ResponsibleUserId == viewerUserId.Value;

        // public: only APPROVED + ACTIVE
        if (!isAdmin && !isOwner)
        {
            if (listing.ModerationStatus != ModerationStatus.Approved || listing.LifecycleStatus != ListingLifecycleStatus.Active)
                return Result<ListingDetailResponse>.Fail(ErrorCodes.Forbidden, "Listing is not public.");
        }

        var imgs = listing.Images
            .OrderBy(i => i.SortOrder)
            .Select(i => new ListingImageDto(i.Id, i.Url, i.SortOrder, i.Caption))
            .ToList();

        return Result<ListingDetailResponse>.Ok(ToDetail(listing, imgs));
    }

    public async Task<Result> UpdateLifecycleAsync(Guid listingId, ListingLifecycleStatus status, bool isAdmin, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Result.Fail(ErrorCodes.Unauthorized, "Unauthorized.");

        var listing = await _db.Listings.FirstOrDefaultAsync(x => x.Id == listingId && !x.IsDeleted, ct);
        if (listing is null) return Result.Fail(ErrorCodes.NotFound, "Listing not found.");

        if (!isAdmin && listing.ResponsibleUserId != userId.Value)
            return Result.Fail(ErrorCodes.Forbidden, "No permission to update lifecycle.");

        // domain transition
        listing.SetLifecycle(status);

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

        // Return real phone
        return Result<string>.Ok(listing.ResponsibleUser.Phone ?? "");
    }

    public async Task<Result<ListingImageDto>> UploadImageAsync(
        Guid listingId,
        Stream content,
        string fileName,
        string contentType,
        int sortOrder,
        string? caption,
        bool isAdmin,
        CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Result<ListingImageDto>.Fail(ErrorCodes.Unauthorized, "Unauthorized.");

        var listing = await _db.Listings
            .FirstOrDefaultAsync(x => x.Id == listingId && !x.IsDeleted, ct);

        if (listing is null) return Result<ListingImageDto>.Fail(ErrorCodes.NotFound, "Listing not found.");

        if (!isAdmin && listing.ResponsibleUserId != userId.Value)
            return Result<ListingImageDto>.Fail(ErrorCodes.Forbidden, "No permission to upload images.");

        var url = await _storage.UploadAsync(content, fileName, contentType, ct);

        var img = new ListingImage
        {
            ListingId = listing.Id,
            Url = url,
            SortOrder = sortOrder,
            Caption = string.IsNullOrWhiteSpace(caption) ? null : caption.Trim()
        };

        _db.ListingImages.Add(img);
        await _db.SaveChangesAsync(true, ct);

        return Result<ListingImageDto>.Ok(new ListingImageDto(img.Id, img.Url, img.SortOrder, img.Caption));
    }

    // -------------------- mapping helpers --------------------
    private static ListingUpdate ToUpdate(CreateListingRequest r) => new(
        Title: r.Title,
        Description: r.Description,
        PropertyType: r.PropertyType,
        PriceAmount: r.PriceAmount,
        PriceCurrency: r.PriceCurrency,
        AreaM2: r.AreaM2,
        Bedrooms: r.Bedrooms,
        Bathrooms: r.Bathrooms,
        FullAddress: r.FullAddress,
        City: r.City,
        District: r.District,
        Ward: r.Ward,
        Street: r.Street,
        Lat: r.Lat,
        Lng: r.Lng,
        VirtualTourUrl: r.VirtualTourUrl
    );

    private static ListingUpdate ToUpdate(UpdateListingRequest r) => new(
        Title: r.Title,
        Description: r.Description,
        PropertyType: r.PropertyType,
        PriceAmount: r.PriceAmount,
        PriceCurrency: r.PriceCurrency,
        AreaM2: r.AreaM2,
        Bedrooms: r.Bedrooms,
        Bathrooms: r.Bathrooms,
        FullAddress: r.FullAddress,
        City: r.City,
        District: r.District,
        Ward: r.Ward,
        Street: r.Street,
        Lat: r.Lat,
        Lng: r.Lng,
        VirtualTourUrl: r.VirtualTourUrl
    );

    private static ListingDetailResponse ToDetail(Listing l, IReadOnlyList<ListingImageDto> images)
    {
        string? maskedPhone = null;
        if (!string.IsNullOrWhiteSpace(l.ResponsibleUser?.Phone))
        {
            var p = l.ResponsibleUser.Phone;
            if (p.Length > 6)
                maskedPhone = p.Substring(0, 3) + "***" + p.Substring(p.Length - 3);
            else
                maskedPhone = p.Substring(0, Math.Max(0, p.Length - 2)) + "**";
        }

        return new ListingDetailResponse(
            l.Id,
            l.Title,
            l.Description,
            l.PropertyType,
            l.Price.Amount,
            l.Price.Currency,
            l.AreaM2,
            l.Bedrooms,
            l.Bathrooms,
            l.Address.FullAddress,
            l.Address.City,
            l.Address.District,
            l.Address.Ward,
            l.Address.Street,
            l.Location?.Lat,
            l.Location?.Lng,
            l.VirtualTourUrl,
            l.ModerationStatus,
            l.ModerationReason,
            l.QualityScore,
            l.LifecycleStatus,
            l.CreatedByUserId,
            l.ResponsibleUserId,
            maskedPhone,
            images
        );
    }
}
