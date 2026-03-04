using SmartEstate.Domain.Common;
using SmartEstate.Domain.Enums;

namespace SmartEstate.Domain.Entities;

public class Payment : AuditableEntity
{
    public Guid PayerUserId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "VND";

    public FeeType FeeType { get; private set; }
    public RefType RefType { get; private set; }
    public Guid? RefId { get; private set; }

    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;
    public string? Description { get; private set; }
    public DateTimeOffset? PaidAt { get; private set; }
    
    // Additional fields not in guide but useful for implementation
    public string? Provider { get; private set; }
    public string? ProviderRef { get; private set; }
    public string? PayUrl { get; private set; }
    public string? RawPayloadJson { get; private set; }

    public static Payment Create(
        Guid payerUserId,
        decimal amount,
        string currency,
        FeeType feeType,
        RefType refType,
        Guid? refId,
        string? description,
        string? provider = null,
        string? payUrl = null)
    {
        if (amount < 0) throw new DomainException("payment amount must be >= 0");

        return new Payment
        {
            PayerUserId = payerUserId,
            Amount = amount,
            Currency = currency ?? "VND",
            FeeType = feeType,
            RefType = refType,
            RefId = refId,
            Description = description,
            Status = PaymentStatus.Pending,
            Provider = provider,
            PayUrl = payUrl
        };
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
