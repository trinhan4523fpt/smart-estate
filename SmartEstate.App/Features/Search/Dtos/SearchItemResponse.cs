using SmartEstate.Domain.Enums;

namespace SmartEstate.App.Features.Search.Dtos;

public sealed record SearchItemResponse(
    Guid Id,
    string Title,
    PropertyType PropertyType,
    TransactionType TransactionType,
    decimal Price,
    double? AreaM2,
    int? Bedrooms,
    int? Bathrooms,
    string? City,
    string? District,
    string? Address,
    decimal? Lat,
    decimal? Lng,
    List<string> Images,
    DateTimeOffset CreatedAt
);
