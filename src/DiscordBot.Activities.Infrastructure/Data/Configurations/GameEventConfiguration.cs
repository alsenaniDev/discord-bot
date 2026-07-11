using DiscordBot.Activities.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiscordBot.Activities.Infrastructure.Data.Configurations;

public class GameEventConfiguration : IEntityTypeConfiguration<GameEvent>
{
    public void Configure(EntityTypeBuilder<GameEvent> builder)
    {
        builder.ToTable("GameEvents");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.GameKey, x.IdempotencyKey }).IsUnique();
        builder.HasIndex(x => new { x.GameSessionId, x.EventType, x.CreatedAtUtc });
        builder.Property(x => x.GameKey).HasMaxLength(64).IsRequired();
        builder.Property(x => x.EventType).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(24).IsRequired();
        builder.Property(x => x.PayloadJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.IdempotencyKey).HasMaxLength(160).IsRequired();
        builder.Property(x => x.DiscordUserId).HasMaxLength(32);
        builder.Property(x => x.ErrorMessage).HasMaxLength(2000);
        builder.HasOne(x => x.GameSession).WithMany(x => x.Events).HasForeignKey(x => x.GameSessionId).OnDelete(DeleteBehavior.Cascade);
    }
}
