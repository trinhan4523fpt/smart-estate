using Microsoft.AspNetCore.Mvc;
using SmartEstate.App.Features.Search;
using SmartEstate.App.Features.Search.Dtos;
using SmartEstate.Domain.Enums;
using SmartEstate.Shared.Errors;

namespace SmartEstate.Api.Controllers;

[ApiController]
[Route("api/search")]
public sealed class SearchController : ControllerBase
{
    private readonly SearchService _svc;

    public SearchController(SearchService svc)
    {
        _svc = svc;
    }

    [HttpGet("listings")]
    public async Task<IActionResult> SearchListings(
        [FromQuery] string? keyword,
        [FromQuery] string? city,
        [FromQuery] string? district,
        [FromQuery] int? propertyType,
        [FromQuery] int? transactionType,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] double? minAreaM2,
        [FromQuery] double? maxAreaM2,
        [FromQuery] int? minBedrooms,
        [FromQuery] int? minBathrooms,
        [FromQuery] double? minLat,
        [FromQuery] double? maxLat,
        [FromQuery] double? minLng,
        [FromQuery] double? maxLng,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sort = "newest",
        CancellationToken ct = default)
    {
        var pt = propertyType is null ? null : (PropertyType?)propertyType.Value;
        var tt = transactionType is null ? null : (TransactionType?)transactionType.Value;

        var req = new SearchRequest(
            Keyword: keyword,
            City: city,
            District: district,
            Ward: null,
            PropertyType: pt,
            TransactionType: tt,
            MinPrice: minPrice,
            MaxPrice: maxPrice,
            MinAreaM2: minAreaM2,
            MaxAreaM2: maxAreaM2,
            MinBedrooms: minBedrooms,
            MinBathrooms: minBathrooms,
            MinLat: minLat,
            MaxLat: maxLat,
            MinLng: minLng,
            MaxLng: maxLng,
            Page: page,
            PageSize: pageSize,
            Sort: sort
        );

        var result = await _svc.SearchAsync(req, ct);
        if (!result.IsSuccess)
            return BadRequest(result.Error ?? new AppError(ErrorCodes.Unexpected, "Unexpected error"));

        // Frontend mock returns Listing[].
        return Ok(result.Value.Items);
    }
}
