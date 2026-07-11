using DiscordBot.Activities.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiscordBot.Activities.Infrastructure.Data.Configurations;

public class RouletteGameSessionConfiguration : IEntityTypeConfiguration<RouletteGameSession>
{
    public void Configure(EntityTypeBuilder<RouletteGameSession> builder)
    {
        builder.ToTable("RouletteGameSessions");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.GameSessionId).IsUnique();
        builder.HasIndex(x => new { x.Status, x.ExpiresAtUtc });
        builder.HasIndex(x => new { x.HostUserDiscordId, x.Status, x.ExpiresAtUtc });
        builder.HasIndex(x => new { x.AnnouncementStatus, x.AnnouncementNextAttemptAtUtc });
        builder.Property(x => x.HostUserDiscordId).HasMaxLength(32).IsRequired();
        builder.Property(x => x.HostUsername).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(24).IsRequired();
        builder.Property(x => x.DiscordAnnouncementChannelId).HasMaxLength(32);
        builder.Property(x => x.DiscordAnnouncementMessageId).HasMaxLength(32);
        builder.Property(x => x.AnnouncementStatus).HasMaxLength(24).HasDefaultValue("NotRequested").IsRequired();
        builder.Property(x => x.AnnouncementLastError).HasMaxLength(2000);
        builder.Property(x => x.CurrentTurnUserDiscordId).HasMaxLength(32);
        builder.Property(x => x.PendingTargetUserDiscordId).HasMaxLength(32);
        builder.Property(x => x.PendingActionStatus).HasMaxLength(32).IsRequired();
        builder.Property(x => x.LastSpinResultJson).HasColumnType("jsonb");
        builder.HasOne(x => x.GameSession).WithOne(x => x.Roulette).HasForeignKey<RouletteGameSession>(x => x.GameSessionId).OnDelete(DeleteBehavior.Cascade);
    }
}
