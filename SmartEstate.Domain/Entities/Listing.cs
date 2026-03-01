using SmartEstate.Domain.Common;
using SmartEstate.Domain.Enums;
using SmartEstate.Domain.ValueObjects;

namespace SmartEstate.Domain.Entities;

public class Listing : AuditableEntity
{
    // -------------------- Core fields --------------------
    public string Title { get; private set; } = default!;
    public string Description { get; private set; } = default!;

    public PropertyType PropertyType { get; private set; }

    public Money Price { get; private set; } = new Money(0);

    public double? AreaM2 { get; private set; }
    public int? Bedrooms { get; private set; }
    public int? Bathrooms { get; private set; }

    public AddressParts Address { get; private set; } = new AddressParts(null, null, null, null, null);
    public GeoPoint? Location { get; private set; }

    public string? VirtualTourUrl { get; private set; }

    // -------------------- Moderation --------------------
    public ModerationStatus ModerationStatus { get; private set; } = ModerationStatus.NeedReview;
    public string? ModerationReason { get; private set; }
    public decimal? QualityScore { get; set; }              // can be set by domain method
    public string? AiFlagsJson { get; set; }                // can be set by domain method

    // -------------------- Lifecycle --------------------
    public ListingLifecycleStatus LifecycleStatus { get; private set; } = ListingLifecycleStatus.Active;

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
    public ICollection<TakeoverRequest> TakeoverRequests { get; set; } = new List<TakeoverRequest>();
    public ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();
    public ICollection<UserListingFavorite> FavoritedByUsers { get; set; } = new List<UserListingFavorite>();


    // -------------------- Factory --------------------
    public static Listing Create(Guid ownerUserId, ListingUpdate u)
    {
        var l = new Listing
        {
            CreatedByUserId = ownerUserId,
            ResponsibleUserId = ownerUserId
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
        if (u.PriceAmount < 0) throw new DomainException("price must be >= 0.");

        Title = u.Title.Trim();
        Description = u.Description.Trim();
        PropertyType = u.PropertyType;

        Price = new Money(u.PriceAmount, u.PriceCurrency);

        AreaM2 = u.AreaM2;
        Bedrooms = u.Bedrooms;
        Bathrooms = u.Bathrooms;

        Address = new AddressParts(u.FullAddress, u.City, u.District, u.Ward, u.Street);

        Location = (u.Lat is not null && u.Lng is not null)
            ? new GeoPoint(u.Lat.Value, u.Lng.Value)
            : null;

        VirtualTourUrl = u.VirtualTourUrl?.Trim();
    }

    // -------------------- Moderation state transitions --------------------
    public void Approve(string? reason = null)
    {
        ModerationStatus = ModerationStatus.Approved;
        ModerationReason = reason;
    }

    public void Reject(string reason)
    {
        Guards.AgainstNullOrEmpty(reason, "moderation reason");
        ModerationStatus = ModerationStatus.Rejected;
        ModerationReason = reason;
    }

    public void NeedReview(string? reason = null)
    {
        ModerationStatus = ModerationStatus.NeedReview;
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
    public void MarkDone() => LifecycleStatus = ListingLifecycleStatus.Done;

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
    public void AssignBroker(Guid brokerUserId) { AssignedBrokerUserId = brokerUserId; ResponsibleUserId = brokerUserId; }
    public void UnassignBroker()
    {
        AssignedBrokerUserId = null;
        ResponsibleUserId = CreatedByUserId;
    }
}
