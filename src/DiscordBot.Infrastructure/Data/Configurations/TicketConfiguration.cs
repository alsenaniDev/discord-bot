using DiscordBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiscordBot.Infrastructure.Data.Configurations;

public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("Tickets");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.GuildId, x.TicketNumber }).IsUnique();
        builder.HasIndex(x => x.ChannelDiscordId).IsUnique();
        builder.HasIndex(x => new { x.GuildId, x.Status });
        builder.Property(x => x.OwnerDiscordUserId).HasMaxLength(32).IsRequired();
        builder.Property(x => x.ChannelDiscordId).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
    }
}
