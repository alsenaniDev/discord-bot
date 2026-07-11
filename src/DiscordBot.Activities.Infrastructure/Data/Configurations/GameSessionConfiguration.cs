using DiscordBot.Activities.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiscordBot.Activities.Infrastructure.Data.Configurations;

public class GameSessionConfiguration : IEntityTypeConfiguration<GameSession>
{
    public void Configure(EntityTypeBuilder<GameSession> builder)
    {
        builder.ToTable("GameSessions");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.DiscordGuildId, x.GameKey, x.Status });
        builder.Property(x => x.GameKey).HasMaxLength(64).IsRequired();
        builder.Property(x => x.GameVersion).HasMaxLength(40).IsRequired();
        builder.Property(x => x.DiscordGuildId).HasMaxLength(32).IsRequired();
        builder.Property(x => x.DiscordChannelId).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(24).IsRequired();
        builder.Property(x => x.ResultJson).HasColumnType("jsonb");
        builder.Property(x => x.RowVersion);
        builder.HasOne(x => x.ActivitySession).WithMany().HasForeignKey(x => x.ActivitySessionId).OnDelete(DeleteBehavior.Restrict);
    }
}
