using SmartEstate.Domain.Common;

namespace SmartEstate.Domain.Entities;

public class PointLedgerEntry : AuditableEntity
{
    public Guid UserId { get; set; }
    public int Delta { get; set; }
    public string Reason { get; set; } = default!;
    public string RefType { get; set; } = default!;
    public Guid? RefId { get; set; }
    public bool IsMonthlyBucket { get; set; }
    public int BalanceMonthlyAfter { get; set; }
    public int BalancePermanentAfter { get; set; }
    public string? Bucket { get; set; }
    public string? MonthKey { get; set; }
    public string? TxType { get; set; }
    public string? Note { get; set; }
}
