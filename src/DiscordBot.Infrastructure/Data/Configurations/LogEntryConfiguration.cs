using DiscordBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiscordBot.Infrastructure.Data.Configurations;

public class LogEntryConfiguration : IEntityTypeConfiguration<LogEntry>
{
    public void Configure(EntityTypeBuilder<LogEntry> builder)
    {
        builder.ToTable("LogEntries");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.GuildId, x.CreatedAt });
        builder.HasIndex(x => new { x.GuildId, x.Type, x.CreatedAt });
        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(64);
        builder.Property(x => x.Message).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.ActorDiscordUserId).HasMaxLength(32);
        builder.Property(x => x.TargetDiscordUserId).HasMaxLength(32);
        builder.Property(x => x.ChannelDiscordId).HasMaxLength(32);
        builder.Property(x => x.MetadataJson).HasMaxLength(4000);
    }
}
