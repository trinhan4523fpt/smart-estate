using Microsoft.EntityFrameworkCore;
using SmartEstate.App.Common.Abstractions;
using SmartEstate.App.Features.Favorites.Dtos;
using SmartEstate.Domain.Entities;
using SmartEstate.Domain.Enums;
using SmartEstate.Infrastructure.Persistence;
using SmartEstate.Shared.Errors;
using SmartEstate.Shared.Paging;
using SmartEstate.Shared.Results;
using SmartEstate.Shared.Time;

namespace SmartEstate.App.Features.Favorites;

public sealed class FavoritesService
{
    private readonly SmartEstateDbContext _db;
    private readonly ICurrentUser _currentUser;

    public FavoritesService(SmartEstateDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result> AddAsync(Guid listingId, bool isAdmin, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Result.Fail(ErrorCodes.Unauthorized, "Unauthorized.");

        var listing = await _db.Listings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == listingId && !x.IsDeleted, ct);

        if (listing is null)
            return Result.Fail(ErrorCodes.NotFound, "Listing not found.");

        var isOwner = listing.ResponsibleUserId == userId;

        if (!isAdmin && !isOwner)
        {
            if (listing.ModerationStatus != ModerationStatus.Approved || listing.LifecycleStatus != ListingLifecycleStatus.Active)
                return Result.Fail(ErrorCodes.Forbidden, "Listing is not public.");
        }

        var exists = await _db.UserListingFavorites
            .AnyAsync(x => x.UserId == userId && x.ListingId == listingId && !x.IsDeleted, ct);

        if (exists)
            return Result.Ok();

        var fav = UserListingFavorite.Create(userId.Value, listingId);

        _db.UserListingFavorites.Add(fav);

        try
        {
            await _db.SaveChangesAsync(true, ct);
        }
        catch (DbUpdateException)
        {
            return Result.Ok();
        }

        return Result.Ok();
    }

    public async Task<Result> RemoveAsync(Guid listingId, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Result.Fail(ErrorCodes.Unauthorized, "Unauthorized.");

        var fav = await _db.UserListingFavorites
            .FirstOrDefaultAsync(x => x.UserId == userId && x.ListingId == listingId && !x.IsDeleted, ct);

        if (fav is null) return Result.Ok();

        fav.IsDeleted = true;
        await _db.SaveChangesAsync(true, ct);

        return Result.Ok();
    }

    public async Task<Result<PagedResult<FavoriteItemResponse>>> ListAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        if (userId is null) return Result<PagedResult<FavoriteItemResponse>>.Fail(ErrorCodes.Unauthorized, "Unauthorized.");

        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 20 : Math.Min(pageSize, 100);

        var q = _db.UserListingFavorites
            .AsNoTracking()
            .Where(x => x.UserId == userId && !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt);

        var total = await q.CountAsync(ct);

        var items = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new FavoriteItemResponse(
                x.ListingId,
                x.Listing.Title,
                x.Listing.PropertyType,
                x.Listing.Price,
                "VND",
                x.Listing.City,
                x.Listing.District,
                x.Listing.Address, // Ward -> Address
                (double?)x.Listing.Lat,
                (double?)x.Listing.Lng,
                x.Listing.Images
                    .Where(i => !i.IsDeleted)
                    .OrderBy(i => i.SortOrder)
                    .Select(i => i.Url)
                    .FirstOrDefault(),
                x.CreatedAt
            ))
            .ToListAsync(ct);

        return Result<PagedResult<FavoriteItemResponse>>.Ok(new PagedResult<FavoriteItemResponse>(items, total, page, pageSize));
    }
}
