using SmartEstate.Domain.Common;
using SmartEstate.Domain.Enums;

namespace SmartEstate.Domain.Entities;

public class BrokerRequest : AuditableEntity
{
    public Guid ListingId { get; private set; }
    public Guid SellerId { get; private set; }      // requester (seller)
    public Guid BrokerId { get; private set; }      // target broker

    public TakeoverStatus Status { get; private set; } = TakeoverStatus.Pending;

    public decimal TakeoverFeeAmount { get; private set; } = 500000;
    public Guid? FeePaidByUserId { get; private set; }
    public FeeStatus FeeStatus { get; private set; } = FeeStatus.Unpaid;

    public DateTimeOffset? RespondedAt { get; private set; }
    public DateTimeOffset? PaidAt { get; private set; }

    // Navigation
    public Listing Listing { get; set; } = default!;
    public User Seller { get; set; } = default!;
    public User Broker { get; set; } = default!;

    public static BrokerRequest Create(
        Guid listingId,
        Guid sellerId,
        Guid brokerId,
        decimal feeAmount)
    {
        if (sellerId == brokerId) throw new DomainException("seller and broker cannot be the same user");

        return new BrokerRequest
        {
            ListingId = listingId,
            SellerId = sellerId,
            BrokerId = brokerId,
            Status = TakeoverStatus.Pending,
            TakeoverFeeAmount = feeAmount
        };
    }

    public void Accept(DateTimeOffset at)
    {
        if (Status != TakeoverStatus.Pending)
            throw new DomainException("only pending request can be accepted");

        Status = TakeoverStatus.Accepted;
        RespondedAt = at;
    }

    public void Reject(DateTimeOffset at)
    {
        if (Status != TakeoverStatus.Pending)
            throw new DomainException("only pending request can be rejected");

        Status = TakeoverStatus.Rejected;
        RespondedAt = at;
    }
    
    public void ConfirmPayment(Guid paidByUserId, DateTimeOffset at)
    {
        if (Status != TakeoverStatus.Accepted)
             throw new DomainException("request must be accepted before payment");
             
        FeeStatus = FeeStatus.Paid;
        FeePaidByUserId = paidByUserId;
        PaidAt = at;
    }
}
