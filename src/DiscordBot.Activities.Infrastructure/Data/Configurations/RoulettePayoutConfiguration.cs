using DiscordBot.Activities.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiscordBot.Activities.Infrastructure.Data.Configurations;

public class RoulettePayoutConfiguration : IEntityTypeConfiguration<RoulettePayout>
{
    public void Configure(EntityTypeBuilder<RoulettePayout> builder)
    {
        builder.ToTable("RoulettePayouts");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.RouletteRoundId, x.DiscordUserId }).IsUnique();
        builder.HasIndex(x => x.IdempotencyKey).IsUnique();
        builder.HasIndex(x => new { x.Status, x.NextAttemptAtUtc });
        builder.HasIndex(x => new { x.Status, x.ProcessingStartedAtUtc });
        builder.Property(x => x.DiscordUserId).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.Currency).HasMaxLength(16).IsRequired();
        builder.Property(x => x.IdempotencyKey).HasMaxLength(180).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.Property(x => x.ProcessingOwner).HasMaxLength(80);
        builder.Property(x => x.LastError).HasMaxLength(500);
        builder.HasOne(x => x.RouletteRound).WithMany(x => x.Payouts).HasForeignKey(x => x.RouletteRoundId).OnDelete(DeleteBehavior.Cascade);
    }
}
