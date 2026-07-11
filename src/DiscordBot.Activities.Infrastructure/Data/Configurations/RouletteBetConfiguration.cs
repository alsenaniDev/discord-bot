using DiscordBot.Activities.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiscordBot.Activities.Infrastructure.Data.Configurations;

public class RouletteBetConfiguration : IEntityTypeConfiguration<RouletteBet>
{
    public void Configure(EntityTypeBuilder<RouletteBet> builder)
    {
        builder.ToTable("RouletteBets");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.RouletteRoundId, x.DiscordUserId, x.IdempotencyKey }).IsUnique();
        builder.Property(x => x.DiscordUserId).HasMaxLength(32).IsRequired();
        builder.Property(x => x.BetType).HasMaxLength(64).IsRequired();
        builder.Property(x => x.BetValue).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.Currency).HasMaxLength(16).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Payout).HasPrecision(18, 2);
        builder.Property(x => x.IdempotencyKey).HasMaxLength(160).IsRequired();
        builder.Property(x => x.WalletReservationId).HasMaxLength(120);
        builder.HasOne(x => x.RouletteRound).WithMany(x => x.Bets).HasForeignKey(x => x.RouletteRoundId).OnDelete(DeleteBehavior.Cascade);
    }
}
