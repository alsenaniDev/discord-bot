using DiscordBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiscordBot.Infrastructure.Data.Configurations;

public class GuildMusicSettingsConfiguration : IEntityTypeConfiguration<GuildMusicSettings>
{
    public void Configure(EntityTypeBuilder<GuildMusicSettings> builder)
    {
        builder.ToTable("GuildMusicSettings");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.GuildId).IsUnique();
        builder.Property(x => x.DjRoleDiscordId).HasMaxLength(32);
        builder.HasOne(x => x.Guild).WithOne(x => x.MusicSettings).HasForeignKey<GuildMusicSettings>(x => x.GuildId).OnDelete(DeleteBehavior.Cascade);
    }
}
