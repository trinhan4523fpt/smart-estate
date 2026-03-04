using SmartEstate.Domain.Common;
using SmartEstate.Domain.Enums;

namespace SmartEstate.Domain.Entities;

public class Listing : AuditableEntity
{
    // -------------------- Core fields --------------------
    public string Title { get; private set; } = default!;
    public string Description { get; private set; } = default!;

    public PropertyType PropertyType { get; private set; }
    public TransactionType TransactionType { get; private set; }

    public decimal Price { get; private set; } // VND

    public double? AreaM2 { get; private set; }
    public int? Bedrooms { get; private set; }
    public int? Bathrooms { get; private set; }

    public string? City { get; private set; }
    public string? District { get; private set; }
    public string? Address { get; private set; }

    public decimal? Lat { get; private set; }
    public decimal? Lng { get; private set; }

    public string? VirtualTourUrl { get; private set; }
    
    // -------------------- Seller Info --------------------
    public string? SellerName { get; private set; }
    public string? SellerPhone { get; private set; }
    public bool IsBrokerManaged { get; private set; }

    // -------------------- Moderation --------------------
    public ModerationStatus ModerationStatus { get; private set; } = ModerationStatus.PendingReview;
    public string? ModerationReason { get; private set; }
    public decimal? QualityScore { get; set; }
    public string? AiFlagsJson { get; set; }

    // -------------------- Lifecycle --------------------
    public ListingLifecycleStatus LifecycleStatus { get; private set; } = ListingLifecycleStatus.Active;
    public DateTimeOffset? ApprovedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    // -------------------- Ownership --------------------
    public Guid CreatedByUserId { get; set; }
    public Guid ResponsibleUserId { get; set; }
    public Guid? AssignedBrokerUserId { get; private set; }

    // -------------------- Navigation --------------------
    public User CreatedByUser { get; set; } = default!;
    public User ResponsibleUser { get; set; } = default!;
    public User? AssignedBrokerUser { get; set; }

    public ICollection<ListingImage> Images { get; set; } = new List<ListingImage>();
    public ICollection<ListingReport> Reports { get; set; } = new List<ListingReport>();
    public ICollection<ModerationReport> ModerationReports { get; set; } = new List<ModerationReport>();
    public ICollection<BrokerRequest> BrokerRequests { get; set; } = new List<BrokerRequest>();
    public ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();
    public ICollection<UserListingFavorite> FavoritedByUsers { get; set; } = new List<UserListingFavorite>();


    // -------------------- Factory --------------------
    public static Listing Create(Guid ownerUserId, ListingUpdate u)
    {
        var l = new Listing
        {
            CreatedByUserId = ownerUserId,
            ResponsibleUserId = ownerUserId,
            IsBrokerManaged = false
        };

        l.Activate();
        l.UpdateDetails(u);
        l.NeedReview("New listing created.");

        return l;
    }

    // -------------------- Update methods --------------------
    public void UpdateDetails(ListingUpdate u)
    {
        Guards.AgainstNullOrEmpty(u.Title, "title");
        Guards.AgainstNullOrEmpty(u.Description, "description");
        if (u.Price < 0) throw new DomainException("price must be >= 0.");

        Title = u.Title.Trim();
        Description = u.Description.Trim();
        PropertyType = u.PropertyType;
        TransactionType = u.TransactionType;

        Price = u.Price;

        AreaM2 = u.AreaM2;
        Bedrooms = u.Bedrooms;
        Bathrooms = u.Bathrooms;

        City = u.City?.Trim();
        District = u.District?.Trim();
        Address = u.Address?.Trim();

        Lat = u.Lat;
        Lng = u.Lng;

        VirtualTourUrl = u.VirtualTourUrl?.Trim();
        
        if (u.SellerName != null) SellerName = u.SellerName.Trim();
        if (u.SellerPhone != null) SellerPhone = u.SellerPhone.Trim();
    }

    // -------------------- Moderation state transitions --------------------
    public void Approve(string? reason = null, DateTimeOffset? at = null)
    {
        ModerationStatus = ModerationStatus.Approved;
        ModerationReason = reason;
        ApprovedAt = at ?? DateTimeOffset.UtcNow;
    }

    public void Reject(string reason)
    {
        Guards.AgainstNullOrEmpty(reason, "moderation reason");
        ModerationStatus = ModerationStatus.Rejected;
        ModerationReason = reason;
    }

    public void NeedReview(string? reason = null)
    {
        ModerationStatus = ModerationStatus.PendingReview;
        ModerationReason = reason;
    }

    public void ApplyModerationDecision(string decision, decimal? qualityScore, string? reason, string? flagsJson)
    {
        switch (decision)
        {
            case "AUTO_APPROVE":
                Approve(reason);
                break;
            case "AUTO_REJECT":
                Reject(reason ?? "Rejected by moderation.");
                break;
            default:
                NeedReview(reason);
                break;
        }

        QualityScore = qualityScore;
        AiFlagsJson = flagsJson;
    }

    // -------------------- Lifecycle state transitions --------------------
    public void MarkDone(DateTimeOffset? at = null)
    {
        LifecycleStatus = ListingLifecycleStatus.Done;
        CompletedAt = at ?? DateTimeOffset.UtcNow;
    }

    public void Cancel() => LifecycleStatus = ListingLifecycleStatus.Cancelled;

    public void Activate() => LifecycleStatus = ListingLifecycleStatus.Active;

    public void SetLifecycle(ListingLifecycleStatus status)
    {
        switch (status)
        {
            case ListingLifecycleStatus.Active:
                Activate();
                break;
            case ListingLifecycleStatus.Done:
                MarkDone();
                break;
            case ListingLifecycleStatus.Cancelled:
                Cancel();
                break;
            default:
                throw new DomainException("invalid lifecycle status");
        }
    }

    // -------------------- Assignment --------------------
    public void AssignBroker(Guid brokerUserId) 
    { 
        AssignedBrokerUserId = brokerUserId; 
        ResponsibleUserId = brokerUserId; 
        IsBrokerManaged = true;
    }
    
    public void UnassignBroker()
    {
        AssignedBrokerUserId = null;
        ResponsibleUserId = CreatedByUserId;
        IsBrokerManaged = false;
    }
}
