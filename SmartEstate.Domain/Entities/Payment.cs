using SmartEstate.Domain.Common;
using SmartEstate.Domain.Enums;
using SmartEstate.Domain.ValueObjects;

namespace SmartEstate.Domain.Entities;

public class Payment : AuditableEntity
{
    public PaymentType Type { get; private set; }
    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;

    public Money Amount { get; private set; } = new Money(0);
    public Guid PayerUserId { get; private set; }
    public Guid? ListingId { get; private set; }
    public Guid? TakeoverRequestId { get; private set; }
    public Guid? PointPurchaseId { get; private set; }

    // Provider info (vnpay/momo/stripe/etc)
    public string Provider { get; private set; } = string.Empty;
    public string? ProviderRef { get; private set; }
    public string? PayUrl { get; private set; }

    // raw payload (webhook/debug)
    public string? RawPayloadJson { get; private set; }
    public DateTimeOffset? PaidAt { get; private set; }

    public static Payment CreateTakeoverFee(
        Guid payerUserId,
        Guid listingId,
        Guid takeoverRequestId,
        decimal amount,
        string currency,
        string provider,
        string? providerRef,
        string? payUrl)
    {
        if (amount < 0) throw new DomainException("payment amount must be >= 0");

        var p = new Payment
        {
            Type = PaymentType.TakeoverFee,
            Status = PaymentStatus.Pending,
            PayerUserId = payerUserId,
            ListingId = listingId,
            TakeoverRequestId = takeoverRequestId,
            Amount = new Money(amount, currency),
            Provider = provider,
            ProviderRef = providerRef,
            PayUrl = payUrl
        };
        return p;
    }

    public static Payment CreatePointPurchasePayment(
        Guid payerUserId,
        Guid pointPurchaseId,
        decimal amount,
        string currency,
        string provider,
        string? providerRef,
        string? payUrl)
    {
        if (amount < 0) throw new DomainException("payment amount must be >= 0");

        var p = new Payment
        {
            Type = PaymentType.Other,
            Status = PaymentStatus.Pending,
            PayerUserId = payerUserId,
            PointPurchaseId = pointPurchaseId,
            Amount = new Money(amount, currency),
            Provider = provider,
            ProviderRef = providerRef,
            PayUrl = payUrl
        };
        return p;
    }

    public void MarkPaid(string? rawPayloadJson = null)
    {
        Status = PaymentStatus.Paid;
        RawPayloadJson = rawPayloadJson;
        PaidAt = DateTimeOffset.UtcNow;
    }

    public void MarkFailed(string? rawPayloadJson = null)
    {
        Status = PaymentStatus.Failed;
        RawPayloadJson = rawPayloadJson;
    }

    public void Cancel(string? rawPayloadJson = null)
    {
        Status = PaymentStatus.Cancelled;
        RawPayloadJson = rawPayloadJson;
    }
}
