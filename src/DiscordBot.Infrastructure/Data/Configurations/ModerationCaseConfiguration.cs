using DiscordBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiscordBot.Infrastructure.Data.Configurations;

public class ModerationCaseConfiguration : IEntityTypeConfiguration<ModerationCase>
{
    public void Configure(EntityTypeBuilder<ModerationCase> builder)
    {
        builder.ToTable("ModerationCases");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.GuildId, x.Type, x.CreatedAt });
        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.TargetDiscordUserId).HasMaxLength(32);
        builder.Property(x => x.ModeratorDiscordUserId).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(512);
        builder.Property(x => x.ChannelDiscordId).HasMaxLength(32);
    }
}
