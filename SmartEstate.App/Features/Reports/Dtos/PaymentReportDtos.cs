using SmartEstate.Domain.Enums;
using System.Text.Json.Serialization;

namespace SmartEstate.App.Features.Reports.Dtos;

public sealed record PointPurchaseTotalsResponse(int Count, decimal TotalAmount, string Currency);

public sealed record PaymentResponse(
    Guid Id,
    Guid PayerUserId,
    string? PayerName,
    string? PayerEmail,
    decimal Amount,
    string Currency,
    string FeeType,
    string RefType,
    Guid? RefId,
    string Status,
    DateTimeOffset? PaidAt,
    DateTimeOffset CreatedAt,
    string? Description
);

public sealed record AdminDashboardStats(
    int TotalListings,
    int TotalUsers,
    int PendingModeration,
    decimal TotalRevenue
);
