using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartEstate.Domain.Entities;

namespace SmartEstate.Infrastructure.Persistence.Configurations;

public class ListingConfiguration : IEntityTypeConfiguration<Listing>
{
    public void Configure(EntityTypeBuilder<Listing> b)
    {
        b.ToTable("listings");
        b.HasKey(x => x.Id);

        b.Property(x => x.Title).HasMaxLength(500).IsRequired();
        b.Property(x => x.Description).IsRequired(); // TEXT

        b.Property(x => x.PropertyType)
            .HasConversion<string>()
            .HasMaxLength(20);

        b.Property(x => x.TransactionType)
            .HasConversion<string>()
            .HasMaxLength(10);

        b.Property(x => x.Price)
            .HasColumnType("numeric(18,0)")
            .IsRequired();

        // Address
        b.Property(x => x.City).HasMaxLength(100);
        b.Property(x => x.District).HasMaxLength(100);
        b.Property(x => x.Address); // TEXT

        // Coordinates
        b.Property(x => x.Lat).HasColumnType("decimal(10,8)");
        b.Property(x => x.Lng).HasColumnType("decimal(11,8)");

        // Seller Info
        b.Property(x => x.SellerName).HasMaxLength(255);
        b.Property(x => x.SellerPhone).HasMaxLength(20);

        // Enums as strings
        b.Property(x => x.ModerationStatus)
            .HasConversion<string>()
            .HasMaxLength(20);

        b.Property(x => x.LifecycleStatus)
            .HasConversion<string>()
            .HasMaxLength(20);

        // JSON columns for moderation
        b.Property(x => x.AiFlagsJson);

        // Relationships
        b.HasOne(x => x.CreatedByUser)
            .WithMany(u => u.CreatedListings)
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.ResponsibleUser)
            .WithMany(u => u.ResponsibleListings)
            .HasForeignKey(x => x.ResponsibleUserId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.AssignedBrokerUser)
            .WithMany()
            .HasForeignKey(x => x.AssignedBrokerUserId)
            .OnDelete(DeleteBehavior.SetNull);

        b.HasMany(x => x.BrokerRequests)
            .WithOne(br => br.Listing)
            .HasForeignKey(br => br.ListingId);
            
        b.HasMany(x => x.Conversations)
            .WithOne(c => c.Listing)
            .HasForeignKey(c => c.ListingId);
            
        b.HasMany(x => x.FavoritedByUsers)
            .WithOne(f => f.Listing)
            .HasForeignKey(f => f.ListingId);
            
        b.HasMany(x => x.Reports)
            .WithOne(r => r.Listing)
            .HasForeignKey(r => r.ListingId);
            
        b.HasMany(x => x.ModerationReports)
            .WithOne(mr => mr.Listing)
            .HasForeignKey(mr => mr.ListingId);
            
        b.HasMany(x => x.Images)
            .WithOne(i => i.Listing)
            .HasForeignKey(i => i.ListingId);
    }
}
