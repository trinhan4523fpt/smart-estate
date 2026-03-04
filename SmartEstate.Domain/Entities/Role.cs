using SmartEstate.Domain.Common;

namespace SmartEstate.Domain.Entities;

public class Role : AuditableEntity<short>
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }

    public Role() { }

    public Role(short id, string name, string description)
    {
        Id = id;
        Name = name;
        Description = description;
    }
}
