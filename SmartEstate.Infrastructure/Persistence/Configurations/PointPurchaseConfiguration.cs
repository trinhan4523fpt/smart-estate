using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartEstate.Domain.Entities;

namespace SmartEstate.Infrastructure.Persistence.Configurations;

public class PointPurchaseConfiguration : IEntityTypeConfiguration<PointPurchase>
{
    public void Configure(EntityTypeBuilder<PointPurchase> b)
    {
        b.ToTable("point_purchases");
        b.HasKey(x => x.Id);

        b.Property(x => x.PriceAmount).HasPrecision(18, 2);
        b.Property(x => x.PriceCurrency).HasMaxLength(8).IsRequired();

        b.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.PointPackage)
            .WithMany()
            .HasForeignKey(x => x.PointPackageId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Payment)
            .WithMany()
            .HasForeignKey(x => x.PaymentId)
            .OnDelete(DeleteBehavior.SetNull);

        b.HasIndex(x => x.UserId);
        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.IsDeleted);
    }
}

