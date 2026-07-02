using DiscordBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiscordBot.Infrastructure.Data.Configurations;

public class TicketTimelineEventConfiguration : IEntityTypeConfiguration<TicketTimelineEvent>
{
    public void Configure(EntityTypeBuilder<TicketTimelineEvent> builder)
    {
        builder.ToTable("TicketTimelineEvents");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.TicketId, x.OccurredAt });
        builder.HasIndex(x => new { x.GuildId, x.OccurredAt });
        builder.HasIndex(x => new { x.TicketId, x.DiscordMessageId })
            .IsUnique()
            .HasFilter("\"DiscordMessageId\" IS NOT NULL");

        builder.Property(x => x.EventType).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.ActorDiscordUserId).HasMaxLength(32);
        builder.Property(x => x.ActorDisplayName).HasMaxLength(128);
        builder.Property(x => x.Content).HasMaxLength(4000);
        builder.Property(x => x.DiscordMessageId).HasMaxLength(32);
        builder.Property(x => x.MetadataJson).HasMaxLength(4000);
    }
}
