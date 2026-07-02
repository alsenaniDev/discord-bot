using DiscordBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiscordBot.Infrastructure.Data.Configurations;

public class ModerationPermissionRoleConfiguration : IEntityTypeConfiguration<ModerationPermissionRole>
{
    public void Configure(EntityTypeBuilder<ModerationPermissionRole> builder)
    {
        builder.ToTable("ModerationPermissionRoles");
        builder.HasIndex(x => new { x.GuildId, x.RoleDiscordId }).IsUnique();
        builder.Property(x => x.RoleDiscordId).HasMaxLength(32).IsRequired();
    }
}
