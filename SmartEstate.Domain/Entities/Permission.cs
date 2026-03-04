using SmartEstate.Domain.Common;

namespace SmartEstate.Domain.Entities;

public class Permission : AuditableEntity<short>
{
    public string Code { get; set; } = default!;
    public string? Description { get; set; }

    public Permission()
    {
    }

    public Permission(short id, string code, string description)
    {
        Id = id;
        Code = code;
        Description = description;
    }
}
