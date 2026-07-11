using DiscordBot.Activities.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiscordBot.Activities.Infrastructure.Data.Configurations;

public class RouletteJoinIntentConfiguration : IEntityTypeConfiguration<RouletteJoinIntent>
{
    public void Configure(EntityTypeBuilder<RouletteJoinIntent> builder)
    {
        builder.ToTable("RouletteJoinIntents");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.UserDiscordId, x.DiscordGuildId, x.DiscordChannelId, x.Status, x.ExpiresAtUtc });
        builder.HasIndex(x => new { x.GameSessionId, x.UserDiscordId, x.Status });
        builder.Property(x => x.DiscordGuildId).HasMaxLength(32).IsRequired();
        builder.Property(x => x.DiscordChannelId).HasMaxLength(32).IsRequired();
        builder.Property(x => x.UserDiscordId).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Username).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(24).IsRequired();
        builder.HasOne(x => x.GameSession).WithMany().HasForeignKey(x => x.GameSessionId).OnDelete(DeleteBehavior.Cascade);
    }
}
