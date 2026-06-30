using DiscordBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiscordBot.Infrastructure.Data.Configurations;

public class ReactionRoleConfiguration : IEntityTypeConfiguration<ReactionRole>
{
    public void Configure(EntityTypeBuilder<ReactionRole> builder)
    {
        builder.ToTable("ReactionRoles");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.ButtonCustomId).IsUnique();
        builder.HasIndex(x => new { x.GuildId, x.IsActive, x.CreatedAt });

        builder.Property(x => x.ChannelDiscordId).HasMaxLength(32).IsRequired();
        builder.Property(x => x.MessageDiscordId).HasMaxLength(32).IsRequired();
        builder.Property(x => x.RoleDiscordId).HasMaxLength(32).IsRequired();
        builder.Property(x => x.ButtonCustomId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.ButtonLabel).HasMaxLength(80).IsRequired();
    }
}
