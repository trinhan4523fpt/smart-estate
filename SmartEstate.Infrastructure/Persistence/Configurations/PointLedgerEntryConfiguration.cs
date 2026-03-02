using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartEstate.Domain.Entities;

namespace SmartEstate.Infrastructure.Persistence.Configurations;

public class PointLedgerEntryConfiguration : IEntityTypeConfiguration<PointLedgerEntry>
{
    public void Configure(EntityTypeBuilder<PointLedgerEntry> b)
    {
        b.ToTable("point_ledger");
        b.HasKey(x => x.Id);

        b.Property(x => x.Reason).HasMaxLength(200).IsRequired();
        b.Property(x => x.RefType).HasMaxLength(50).IsRequired();
        b.Property(x => x.Bucket).HasMaxLength(20);
        b.Property(x => x.MonthKey).HasMaxLength(7);
        b.Property(x => x.TxType).HasMaxLength(50);
        b.Property(x => x.Note).HasMaxLength(500);

        b.HasIndex(x => x.UserId);
        b.HasIndex(x => new { x.UserId, x.CreatedAt });
        b.HasIndex(x => new { x.Bucket, x.MonthKey });
        b.HasIndex(x => x.TxType);
        b.HasIndex(x => x.IsDeleted);
    }
}
