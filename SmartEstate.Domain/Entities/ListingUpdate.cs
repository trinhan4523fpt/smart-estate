using SmartEstate.Domain.Enums;

namespace SmartEstate.Domain.Entities;

public sealed record ListingUpdate(
    string Title,
    string Description,
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
    string? VirtualTourUrl,
    string? SellerName,
    string? SellerPhone
);
