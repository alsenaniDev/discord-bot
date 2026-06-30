using DiscordBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiscordBot.Infrastructure.Data.Configurations;

public class WarningConfiguration : IEntityTypeConfiguration<Warning>
{
    public void Configure(EntityTypeBuilder<Warning> builder)
    {
        builder.ToTable("Warnings");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.GuildId, x.TargetDiscordUserId });
        builder.Property(x => x.TargetDiscordUserId).HasMaxLength(32).IsRequired();
        builder.Property(x => x.ModeratorDiscordUserId).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(512).IsRequired();
    }
}
