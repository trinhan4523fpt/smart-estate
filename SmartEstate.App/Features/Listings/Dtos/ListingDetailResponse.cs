using SmartEstate.Domain.Enums;
using System.Text.Json.Serialization;

namespace SmartEstate.App.Features.Listings.Dtos;

public sealed record ListingDetailResponse(
    Guid Id,
    string Title,
    string Description,
    [property: JsonPropertyName("type")] PropertyType PropertyType,
    [property: JsonPropertyName("transaction")] TransactionType TransactionType,
    decimal Price,
    [property: JsonPropertyName("area")] double? AreaM2,
    int? Bedrooms,
    int? Bathrooms,
    string? City,
    string? District,
    string? Address,
    decimal? Lat,
    decimal? Lng,
    string? VirtualTourUrl,
    
    string? SellerName,
    string? SellerPhone,
    bool IsBrokerManaged,
    
    ModerationStatus ModerationStatus,
    string? ModerationReason,
    ListingLifecycleStatus LifecycleStatus,
    
    Guid CreatedByUserId,
    Guid ResponsibleUserId,
    
    List<string> Images,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    DateTimeOffset? ApprovedAt,
    
    // Optional details
    object? BrokerRequests = null,
    object? Reports = null
);
