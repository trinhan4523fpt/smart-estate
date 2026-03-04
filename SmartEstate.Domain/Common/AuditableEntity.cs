namespace SmartEstate.Domain.Common;

public abstract class AuditableEntity<TKey> : BaseEntity<TKey>
{
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
}

public abstract class AuditableEntity : AuditableEntity<Guid>
{
    protected AuditableEntity() => Id = Guid.NewGuid();
}
