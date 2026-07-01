using DiscordBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiscordBot.Infrastructure.Data.Configurations;

public class DiscordGuildMemberConfiguration : IEntityTypeConfiguration<DiscordGuildMember>
{
    public void Configure(EntityTypeBuilder<DiscordGuildMember> builder)
    {
        builder.ToTable("DiscordGuildMembers");

        builder.HasIndex(x => new { x.GuildId, x.DiscordUserId }).IsUnique();
        builder.Property(x => x.DiscordUserId).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Username).HasMaxLength(128).IsRequired();
        builder.Property(x => x.GlobalName).HasMaxLength(128);
        builder.Property(x => x.Nickname).HasMaxLength(128);
        builder.Property(x => x.DiscordRoleIdsJson).HasMaxLength(4000).IsRequired();
    }
}
