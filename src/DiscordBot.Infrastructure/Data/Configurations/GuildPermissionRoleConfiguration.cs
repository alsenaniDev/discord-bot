using DiscordBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiscordBot.Infrastructure.Data.Configurations;

public class GuildPermissionRoleConfiguration : IEntityTypeConfiguration<GuildPermissionRole>
{
    public void Configure(EntityTypeBuilder<GuildPermissionRole> builder)
    {
        builder.ToTable("GuildPermissionRoles");

        builder.HasIndex(x => new { x.GuildId, x.DiscordRoleId }).IsUnique();
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.DiscordRoleId).HasMaxLength(32).IsRequired();
    }
}
