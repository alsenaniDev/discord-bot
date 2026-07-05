using DiscordBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiscordBot.Infrastructure.Data.Configurations;

public class GuildPanelButtonConfiguration : IEntityTypeConfiguration<GuildPanelButton>
{
    public void Configure(EntityTypeBuilder<GuildPanelButton> builder)
    {
        builder.ToTable("GuildPanelButtons");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.PanelId);
        builder.Property(x => x.Label).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Emoji).HasMaxLength(100);
        builder.Property(x => x.Style).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.ActionType).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Url).HasMaxLength(512);
        builder.Property(x => x.ResponseMessage).HasMaxLength(2000);
        builder.Property(x => x.RoleDiscordId).HasMaxLength(32);
        builder.HasOne<GuildWorkflow>().WithMany().HasForeignKey(x => x.WorkflowId).OnDelete(DeleteBehavior.SetNull);
    }
}
