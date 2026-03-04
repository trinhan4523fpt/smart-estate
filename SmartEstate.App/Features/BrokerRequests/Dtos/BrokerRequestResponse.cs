using System.Text.Json.Serialization;

namespace SmartEstate.App.Features.BrokerRequests.Dtos;

public sealed record BrokerRequestResponse(
    Guid Id,
    Guid ListingId,
    Guid SellerId,
    string? SellerName,
    Guid BrokerId,
    string? BrokerName,
    string Status,
    decimal TakeoverFeeAmount,
    Guid? FeePaidByUserId,
    string FeeStatus,
    [property: JsonPropertyName("requestedAt")] DateTimeOffset CreatedAt,
    DateTimeOffset? RespondedAt,
    DateTimeOffset? PaidAt
);
