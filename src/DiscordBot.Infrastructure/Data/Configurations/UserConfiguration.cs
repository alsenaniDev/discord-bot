using DiscordBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiscordBot.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.DiscordUserId).IsUnique();
        builder.Property(x => x.DiscordUserId).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Username).HasMaxLength(128).IsRequired();
        builder.Property(x => x.GlobalName).HasMaxLength(128);
        builder.Property(x => x.AvatarUrl).HasMaxLength(512);
    }
}
