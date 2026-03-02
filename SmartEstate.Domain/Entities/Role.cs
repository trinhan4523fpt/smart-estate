using SmartEstate.Domain.Common;

namespace SmartEstate.Domain.Entities;

public class Role : AuditableEntity
{
    public new short Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
}
