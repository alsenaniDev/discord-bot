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
        builder.Property(x => x.TicketWelcomeTitle).HasMaxLength(256);
        builder.Property(x => x.TicketWelcomeMessage).HasMaxLength(2000);
        builder.Property(x => x.TicketClosedMessage).HasMaxLength(2000);
        builder.Property(x => x.TicketClosedFromDashboardMessage).HasMaxLength(2000);
        builder.Property(x => x.TicketStaffReplyPrefix).HasMaxLength(500);
        builder.Property(x => x.CommandPanelChannelId).HasMaxLength(32);
        builder.Property(x => x.CommandPanelMessageId).HasMaxLength(32);
        builder.Property(x => x.CommandPanelTitle).HasMaxLength(256);
        builder.Property(x => x.CommandPanelDescription).HasMaxLength(2000);
        builder.Property(x => x.CommandPanelButtonsJson).HasMaxLength(4000);
    }
}
