using DiscordBot.Activities.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiscordBot.Activities.Infrastructure.Data.Configurations;

public class GameResultConfiguration : IEntityTypeConfiguration<GameResult>
{
    public void Configure(EntityTypeBuilder<GameResult> builder)
    {
        builder.ToTable("GameResults");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.GameSessionId, x.DiscordUserId }).IsUnique();
        builder.Property(x => x.GameKey).HasMaxLength(64).IsRequired();
        builder.Property(x => x.DiscordUserId).HasMaxLength(32).IsRequired();
        builder.Property(x => x.ResultJson).HasColumnType("jsonb").IsRequired();
        builder.HasOne(x => x.GameSession).WithMany().HasForeignKey(x => x.GameSessionId).OnDelete(DeleteBehavior.Cascade);
    }
}
