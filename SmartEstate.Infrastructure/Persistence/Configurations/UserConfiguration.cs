using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartEstate.Domain.Entities;

namespace SmartEstate.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("users");
        b.HasKey(x => x.Id);

        b.Property(x => x.Email).HasMaxLength(256).IsRequired();
        b.HasIndex(x => x.Email).IsUnique();

        b.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
        b.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
        b.Property(x => x.Phone).HasMaxLength(20);
        b.Property(x => x.Avatar).HasMaxLength(1000);
        b.Property(x => x.Address).HasMaxLength(500);

        b.Property(x => x.Role)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
            
        b.Property(x => x.IsActive).IsRequired();

        // soft delete
        b.HasIndex(x => x.IsDeleted);

        // created listings
        b.HasMany(x => x.CreatedListings)
            .WithOne(x => x.CreatedByUser)
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // responsible listings
        b.HasMany(x => x.ResponsibleListings)
            .WithOne(x => x.ResponsibleUser)
            .HasForeignKey(x => x.ResponsibleUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // conversations as buyer
        b.HasMany(x => x.BuyerConversations)
            .WithOne(x => x.BuyerUser)
            .HasForeignKey(x => x.BuyerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // messages sent
        b.HasMany(x => x.MessagesSent)
            .WithOne(x => x.SenderUser)
            .HasForeignKey(x => x.SenderUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
