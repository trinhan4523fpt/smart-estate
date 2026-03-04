using Microsoft.EntityFrameworkCore;
using SmartEstate.App.Features.Search.Dtos;
using SmartEstate.Domain.Enums;
using SmartEstate.Infrastructure.Persistence;
using SmartEstate.Shared.Errors;
using SmartEstate.Shared.Paging;
using SmartEstate.Shared.Results;
using SmartEstate.Shared.Time;

namespace SmartEstate.App.Features.Search;

public sealed class SearchService
{
    private readonly SmartEstateDbContext _db;
    private readonly IClock _clock;

    public SearchService(SmartEstateDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<Result<PagedResult<SearchItemResponse>>> SearchAsync(SearchRequest req, CancellationToken ct = default)
    {
        var page = req.Page <= 0 ? 1 : req.Page;
        var pageSize = req.PageSize <= 0 ? 20 : Math.Min(req.PageSize, 100);

        var q = _db.Listings
            .AsNoTracking()
            .Where(x => !x.IsDeleted
                && x.ModerationStatus == ModerationStatus.Approved
                && x.LifecycleStatus == ListingLifecycleStatus.Active);

        if (!string.IsNullOrWhiteSpace(req.Keyword))
        {
            var k = req.Keyword.Trim();
            q = q.Where(x => x.Title.Contains(k) || x.Description.Contains(k));
        }

        if (!string.IsNullOrWhiteSpace(req.City)) q = q.Where(x => x.City == req.City.Trim());
        if (!string.IsNullOrWhiteSpace(req.District)) q = q.Where(x => x.District == req.District.Trim());
        
        if (req.PropertyType.HasValue) q = q.Where(x => x.PropertyType == req.PropertyType.Value);
        if (req.TransactionType.HasValue) q = q.Where(x => x.TransactionType == req.TransactionType.Value);

        if (req.MinPrice.HasValue) q = q.Where(x => x.Price >= req.MinPrice.Value);
        if (req.MaxPrice.HasValue) q = q.Where(x => x.Price <= req.MaxPrice.Value);
        
        if (req.MinAreaM2.HasValue) q = q.Where(x => x.AreaM2 >= req.MinAreaM2.Value);
        if (req.MaxAreaM2.HasValue) q = q.Where(x => x.AreaM2 <= req.MaxAreaM2.Value);
        
        if (req.MinBedrooms.HasValue) q = q.Where(x => x.Bedrooms >= req.MinBedrooms.Value);
        if (req.MinBathrooms.HasValue) q = q.Where(x => x.Bathrooms >= req.MinBathrooms.Value);

        // Map bounds
        if (req.MinLat.HasValue && req.MaxLat.HasValue && req.MinLng.HasValue && req.MaxLng.HasValue)
        {
            var minLat = (decimal)req.MinLat.Value;
            var maxLat = (decimal)req.MaxLat.Value;
            var minLng = (decimal)req.MinLng.Value;
            var maxLng = (decimal)req.MaxLng.Value;
            
            q = q.Where(x => x.Lat >= minLat && x.Lat <= maxLat && x.Lng >= minLng && x.Lng <= maxLng);
        }

        // Sorting
        q = req.Sort?.ToLowerInvariant() switch
        {
            "price_asc" => q.OrderBy(x => x.Price),
            "price_desc" => q.OrderByDescending(x => x.Price),
            _ => q.OrderByDescending(x => x.CreatedAt)
        };

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new SearchItemResponse(
                x.Id,
                x.Title,
                x.PropertyType,
                x.TransactionType,
                x.Price,
                x.AreaM2,
                x.Bedrooms,
                x.Bathrooms,
                x.City,
                x.District,
                x.Address,
                x.Lat,
                x.Lng,
                x.Images.Where(i => !i.IsDeleted).OrderBy(i => i.SortOrder).Select(i => i.Url).ToList(),
                x.CreatedAt
            ))
            .ToListAsync(ct);

        return Result<PagedResult<SearchItemResponse>>.Ok(new PagedResult<SearchItemResponse>(items, total, page, pageSize));
    }
}
