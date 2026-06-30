using DiscordBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiscordBot.Infrastructure.Data.Configurations;

public class DiscordChannelConfiguration : IEntityTypeConfiguration<DiscordChannel>
{
    public void Configure(EntityTypeBuilder<DiscordChannel> builder)
    {
        builder.ToTable("DiscordChannels");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.GuildId, x.DiscordChannelId }).IsUnique();
        builder.HasIndex(x => new { x.GuildId, x.Type });
        builder.Property(x => x.DiscordChannelId).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(16);
    }
}
