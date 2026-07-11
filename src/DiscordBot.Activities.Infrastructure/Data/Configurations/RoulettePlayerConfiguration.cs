using DiscordBot.Activities.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiscordBot.Activities.Infrastructure.Data.Configurations;

public class RoulettePlayerConfiguration : IEntityTypeConfiguration<RoulettePlayer>
{
    public void Configure(EntityTypeBuilder<RoulettePlayer> builder)
    {
        builder.ToTable("RoulettePlayers");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.RouletteGameSessionId, x.DiscordUserId }).IsUnique();
        builder.Property(x => x.DiscordUserId).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Username).HasMaxLength(120).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(120);
        builder.Property(x => x.AvatarUrl).HasMaxLength(512);
        builder.HasOne(x => x.RouletteGameSession).WithMany(x => x.Players).HasForeignKey(x => x.RouletteGameSessionId).OnDelete(DeleteBehavior.Cascade);
    }
}
