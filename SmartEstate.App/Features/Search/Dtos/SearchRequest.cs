using SmartEstate.Domain.Enums;

namespace SmartEstate.App.Features.Search.Dtos;

public sealed record SearchRequest(
    string? Keyword,
    string? City,
    string? District,
    string? Ward,
    PropertyType? PropertyType,
    TransactionType? TransactionType,
    decimal? MinPrice,
    decimal? MaxPrice,
    double? MinAreaM2,
    double? MaxAreaM2,
    int? MinBedrooms,
    int? MinBathrooms,
    // map bounds
    double? MinLat,
    double? MaxLat,
    double? MinLng,
    double? MaxLng,
    // paging
    int Page = 1,
    int PageSize = 20,
    string Sort = "newest"
);
