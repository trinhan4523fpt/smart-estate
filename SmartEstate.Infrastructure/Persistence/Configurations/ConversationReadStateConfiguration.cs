using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartEstate.Domain.Entities;

namespace SmartEstate.Infrastructure.Persistence.Configurations;

public class ConversationReadStateConfiguration : IEntityTypeConfiguration<ConversationReadState>
{
    public void Configure(EntityTypeBuilder<ConversationReadState> b)
    {
        b.ToTable("conversation_read_states");
        b.HasKey(x => x.Id);

        b.HasOne(x => x.Conversation)
            .WithMany()
            .HasForeignKey(x => x.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => new { x.ConversationId, x.UserId }).IsUnique();
        b.HasIndex(x => x.IsDeleted);
    }
}

