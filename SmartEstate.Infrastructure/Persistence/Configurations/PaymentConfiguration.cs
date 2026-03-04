using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartEstate.Domain.Entities;

namespace SmartEstate.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");
        
        builder.Property(p => p.Amount)
            .HasColumnType("numeric(18,0)");
            
        builder.Property(p => p.FeeType)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(p => p.RefType)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(20);
    }
}
