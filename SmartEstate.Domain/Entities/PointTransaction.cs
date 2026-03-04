using SmartEstate.Domain.Common;
using SmartEstate.Domain.Enums;

namespace SmartEstate.Domain.Entities;

public class PointTransaction : AuditableEntity
{
    public Guid UserId { get; set; }
    public Guid PackageId { get; set; }
    public int Points { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "VND";
    public PointTransactionStatus Status { get; set; } = PointTransactionStatus.Pending;
    public Guid? PaymentId { get; set; }

    public User User { get; set; } = default!;
    public PointPackage PointPackage { get; set; } = default!;
    public Payment? Payment { get; set; }
}
