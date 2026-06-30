using DiscordBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiscordBot.Infrastructure.Data.Configurations;

public class DiscordRoleConfiguration : IEntityTypeConfiguration<DiscordRole>
{
    public void Configure(EntityTypeBuilder<DiscordRole> builder)
    {
        builder.ToTable("DiscordRoles");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.GuildId, x.DiscordRoleId }).IsUnique();
        builder.Property(x => x.DiscordRoleId).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(256).IsRequired();
    }
}
