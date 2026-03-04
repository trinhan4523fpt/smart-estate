using SmartEstate.Domain.Common;

namespace SmartEstate.Domain.Entities;

public class Message : AuditableEntity
{
    public Guid ConversationId { get; private set; }
    public Guid SenderUserId { get; private set; }

    public string Content { get; private set; } = default!;
    public DateTimeOffset SentAt { get; set; } = DateTimeOffset.UtcNow;

    public bool IsRead { get; set; }
    public DateTimeOffset? ReadAt { get; set; }

    // optional attachments
    public string? AttachmentUrl { get; set; }

    // Navigation
    public Conversation Conversation { get; set; } = default!;
    public User SenderUser { get; set; } = default!;

    public static Message Create(Guid conversationId, Guid senderId, string content)
    {
        Guards.AgainstNullOrEmpty(content, "content");
        return new Message
        {
            ConversationId = conversationId,
            SenderUserId = senderId,
            Content = content,
            SentAt = DateTimeOffset.UtcNow
        };
    }
}
