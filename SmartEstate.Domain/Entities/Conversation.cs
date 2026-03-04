using SmartEstate.Domain.Common;

namespace SmartEstate.Domain.Entities;

public class Conversation : AuditableEntity
{
    public Guid ListingId { get; private set; }

    // buyer initiates conversation
    public Guid BuyerUserId { get; private set; }

    // snapshot of responsible at creation (optional, but useful)
    public Guid ResponsibleUserId { get; set; }

    public DateTimeOffset? BuyerLastReadAt { get; private set; }
    public DateTimeOffset? ResponsibleLastReadAt { get; private set; }

    public DateTimeOffset? LastMessageAt { get; private set; }
    public string? LastMessagePreview { get; private set; }

    // Navigation
    public Listing Listing { get; set; } = default!;
    public User BuyerUser { get; set; } = default!;
    public User ResponsibleUser { get; set; } = default!;

    public ICollection<Message> Messages { get; set; } = new List<Message>();

    public static Conversation Create(Guid listingId, Guid buyerUserId, Guid responsibleUserId)
    {
        return new Conversation
        {
            ListingId = listingId,
            BuyerUserId = buyerUserId,
            ResponsibleUserId = responsibleUserId,
            LastMessageAt = DateTimeOffset.UtcNow
        };
    }

    public void UpdateLastMessage(string preview, DateTimeOffset at)
    {
        LastMessagePreview = preview;
        LastMessageAt = at;
    }

    public void MarkRead(Guid userId)
    {
        if (userId == BuyerUserId)
        {
            BuyerLastReadAt = DateTimeOffset.UtcNow;
        }
        else if (userId == ResponsibleUserId)
        {
            ResponsibleLastReadAt = DateTimeOffset.UtcNow;
        }
    }
}
