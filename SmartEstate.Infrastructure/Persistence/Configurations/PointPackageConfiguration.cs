using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartEstate.Domain.Entities;

namespace SmartEstate.Infrastructure.Persistence.Configurations;

public class PointPackageConfiguration : IEntityTypeConfiguration<PointPackage>
{
    public void Configure(EntityTypeBuilder<PointPackage> b)
    {
        b.ToTable("point_packages");
        b.HasKey(x => x.Id);

        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.PriceAmount).HasColumnType("decimal(18,2)");
        b.Property(x => x.PriceCurrency).HasMaxLength(8).IsRequired();

        b.HasIndex(x => x.IsActive);
        b.HasIndex(x => x.IsDeleted);
    }
}
