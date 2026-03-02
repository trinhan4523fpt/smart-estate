using SmartEstate.Domain.Common;

namespace SmartEstate.Domain.Entities;

public class Permission : AuditableEntity
{
    public new short Id { get; set; }
    public string Code { get; set; } = default!;
    public string? Description { get; set; }
}
