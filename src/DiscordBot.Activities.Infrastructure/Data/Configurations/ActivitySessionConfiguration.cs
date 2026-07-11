using DiscordBot.Activities.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiscordBot.Activities.Infrastructure.Data.Configurations;

public class ActivitySessionConfiguration : IEntityTypeConfiguration<ActivitySession>
{
    public void Configure(EntityTypeBuilder<ActivitySession> builder)
    {
        builder.ToTable("ActivitySessions");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.DiscordGuildId, x.DiscordChannelId, x.GameKey, x.Status });
        builder.HasIndex(x => new { x.DiscordUserId, x.Status, x.ExpiresAtUtc });
        builder.HasIndex(x => new { x.DiscordGuildId, x.DiscordChannelId, x.DiscordActivityInstanceId, x.GameKey });
        builder.Property(x => x.DiscordUserId).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Username).HasMaxLength(120).IsRequired();
        builder.Property(x => x.AvatarUrl).HasMaxLength(512);
        builder.Property(x => x.DiscordGuildId).HasMaxLength(32).IsRequired();
        builder.Property(x => x.DiscordChannelId).HasMaxLength(32).IsRequired();
        builder.Property(x => x.DiscordActivityInstanceId).HasMaxLength(128);
        builder.Property(x => x.GameKey).HasMaxLength(64).IsRequired();
        builder.Property(x => x.GameVersion).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Mode).HasMaxLength(24).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(24).IsRequired();
    }
}
