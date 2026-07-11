using DiscordBot.Activities.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiscordBot.Activities.Infrastructure.Data.Configurations;

public class RouletteRoundConfiguration : IEntityTypeConfiguration<RouletteRound>
{
    public void Configure(EntityTypeBuilder<RouletteRound> builder)
    {
        builder.ToTable("RouletteRounds");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.RouletteGameSessionId, x.RoundNumber }).IsUnique();
        builder.HasIndex(x => new { x.RouletteGameSessionId, x.IdempotencyKey }).IsUnique();
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.Property(x => x.SpinnerUserDiscordId).HasMaxLength(32).IsRequired();
        builder.Property(x => x.TargetUserDiscordId).HasMaxLength(32);
        builder.Property(x => x.IdempotencyKey).HasMaxLength(160).IsRequired();
        builder.Property(x => x.ResultJson).HasColumnType("jsonb").IsRequired();
        builder.HasOne(x => x.RouletteGameSession).WithMany(x => x.Rounds).HasForeignKey(x => x.RouletteGameSessionId).OnDelete(DeleteBehavior.Cascade);
    }
}
