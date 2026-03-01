using SmartEstate.Domain.Common;

namespace SmartEstate.Domain.Entities;

public class ConversationReadState : AuditableEntity
{
    public Guid ConversationId { get; set; }
    public Guid UserId { get; set; }
    public Guid? LastReadMessageId { get; set; }
    public DateTimeOffset? LastReadAt { get; set; }

    public Conversation Conversation { get; set; } = default!;
    public User User { get; set; } = default!;
}

