namespace SmartEstate.App.Features.Points.Dtos;

public sealed record PointPackageDto(Guid Id, string Name, int Points, decimal PriceAmount, string PriceCurrency);

public sealed record CreatePointPaymentRequest(Guid PointPackageId);

public sealed record PointPaymentResponse(string PaymentUrl);

public sealed record PointTransactionHistoryResponse(
    Guid Id,
    string PackageName,
    int Points,
    decimal Amount,
    string Status,
    DateTimeOffset CreatedAt
);

public sealed record PointBalanceResponse(int Balance, int TotalSpent);
