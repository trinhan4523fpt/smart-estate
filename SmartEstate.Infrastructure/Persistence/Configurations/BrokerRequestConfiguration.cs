using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartEstate.Domain.Entities;

namespace SmartEstate.Infrastructure.Persistence.Configurations;

public class BrokerRequestConfiguration : IEntityTypeConfiguration<BrokerRequest>
{
    public void Configure(EntityTypeBuilder<BrokerRequest> builder)
    {
        builder.ToTable("broker_requests");
        
        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20); // pending, accepted, rejected

        builder.Property(x => x.FeeStatus)
            .HasConversion<string>()
            .HasMaxLength(10); // unpaid, paid

        builder.Property(x => x.TakeoverFeeAmount)
            .HasColumnType("numeric(18,0)");

        builder.HasOne(x => x.Listing)
            .WithMany(l => l.BrokerRequests)
            .HasForeignKey(x => x.ListingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Seller)
            .WithMany()
            .HasForeignKey(x => x.SellerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Broker)
            .WithMany()
            .HasForeignKey(x => x.BrokerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
