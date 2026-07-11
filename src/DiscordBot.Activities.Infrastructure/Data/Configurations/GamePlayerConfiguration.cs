using DiscordBot.Activities.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiscordBot.Activities.Infrastructure.Data.Configurations;

public class GamePlayerConfiguration : IEntityTypeConfiguration<GamePlayer>
{
    public void Configure(EntityTypeBuilder<GamePlayer> builder)
    {
        builder.ToTable("GamePlayers");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.GameSessionId, x.DiscordUserId }).IsUnique();
        builder.Property(x => x.DiscordUserId).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Username).HasMaxLength(120).IsRequired();
        builder.Property(x => x.AvatarUrl).HasMaxLength(512);
        builder.Property(x => x.Status).HasMaxLength(24).IsRequired();
        builder.HasOne(x => x.GameSession).WithMany(x => x.Players).HasForeignKey(x => x.GameSessionId).OnDelete(DeleteBehavior.Cascade);
    }
}
