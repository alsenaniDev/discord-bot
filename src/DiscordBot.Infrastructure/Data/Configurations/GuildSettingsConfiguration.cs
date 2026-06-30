using DiscordBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiscordBot.Infrastructure.Data.Configurations;

public class GuildSettingsConfiguration : IEntityTypeConfiguration<GuildSettings>
{
    public void Configure(EntityTypeBuilder<GuildSettings> builder)
    {
        builder.ToTable("GuildSettings");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.GuildId).IsUnique();
        builder.Property(x => x.WelcomeChannelId).HasMaxLength(32);
        builder.Property(x => x.WelcomeMessage).HasMaxLength(2000);
        builder.Property(x => x.AutoRoleId).HasMaxLength(32);
        builder.Property(x => x.LogChannelId).HasMaxLength(32);
        builder.Property(x => x.TicketCategoryId).HasMaxLength(32);
    }
}
