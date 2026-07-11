using DiscordBot.Activities.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiscordBot.Activities.Infrastructure.Data.Configurations;

public class GameWalletTransactionConfiguration : IEntityTypeConfiguration<GameWalletTransaction>
{
    public void Configure(EntityTypeBuilder<GameWalletTransaction> builder)
    {
        builder.ToTable("GameWalletTransactions");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.GameSessionId, x.IdempotencyKey }).IsUnique();
        builder.HasIndex(x => new { x.DiscordGuildId, x.DiscordUserId, x.CreatedAtUtc });
        builder.Property(x => x.DiscordGuildId).HasMaxLength(32).IsRequired();
        builder.Property(x => x.DiscordUserId).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.Currency).HasMaxLength(16).IsRequired();
        builder.Property(x => x.Type).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(24).IsRequired();
        builder.Property(x => x.IdempotencyKey).HasMaxLength(160).IsRequired();
        builder.Property(x => x.PlatformReservationId).HasMaxLength(120);
        builder.Property(x => x.ErrorMessage).HasMaxLength(2000);
        builder.HasOne(x => x.GameSession).WithMany().HasForeignKey(x => x.GameSessionId).OnDelete(DeleteBehavior.Cascade);
    }
}
