using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartEstate.Domain.Entities;

namespace SmartEstate.Infrastructure.Persistence.Configurations;

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> b)
    {
        b.ToTable("payments");

        b.HasKey(x => x.Id);

        b.Property(x => x.Type).IsRequired();
        b.Property(x => x.Status).IsRequired();
        b.Property(x => x.Provider).HasMaxLength(50).IsRequired();
        b.Property(x => x.ProviderRef).HasMaxLength(200);
        b.Property(x => x.PayUrl).HasMaxLength(1000);
        b.Property(x => x.RawPayloadJson).HasColumnType("nvarchar(max)");
        b.Property(x => x.PaidAt);

        b.OwnsOne(x => x.Amount, money =>
        {
            money.Property(p => p.Amount).HasColumnName("amount").HasColumnType("decimal(18,2)").IsRequired();
            money.Property(p => p.Currency).HasColumnName("currency").HasMaxLength(8).IsRequired();
        });

        b.HasIndex(x => new { x.Type, x.Status });
        b.HasIndex(x => x.PayerUserId);
        b.HasIndex(x => x.TakeoverRequestId);
        b.HasIndex(x => x.PointPurchaseId);
        b.HasIndex(x => new { x.Provider, x.ProviderRef }).IsUnique(false);
    }
}
