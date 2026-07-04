using DiscordBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiscordBot.Infrastructure.Data.Configurations;

public class GuildPanelConfiguration : IEntityTypeConfiguration<GuildPanel>
{
    public void Configure(EntityTypeBuilder<GuildPanel> builder)
    {
        builder.ToTable("GuildPanels");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.GuildId);
        builder.HasIndex(x => x.ChannelDiscordId);
        builder.HasIndex(x => x.MessageDiscordId);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.ImageUrl).HasMaxLength(512);
        builder.Property(x => x.ChannelDiscordId).HasMaxLength(32).IsRequired();
        builder.Property(x => x.MessageDiscordId).HasMaxLength(32);
        builder.Property(x => x.LastPublishFailureReason).HasMaxLength(1000);
        builder.HasMany(x => x.Buttons).WithOne(x => x.Panel).HasForeignKey(x => x.PanelId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
