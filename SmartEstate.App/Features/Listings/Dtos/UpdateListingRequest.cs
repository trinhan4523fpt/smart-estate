using SmartEstate.Domain.Enums;

namespace SmartEstate.App.Features.Listings.Dtos;

public sealed record UpdateListingRequest(
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
    List<string>? Images
);
