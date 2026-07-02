using DiscordBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiscordBot.Infrastructure.Data.Configurations;

public class GuildConfiguration : IEntityTypeConfiguration<Guild>
{
    public void Configure(EntityTypeBuilder<Guild> builder)
    {
        builder.ToTable("Guilds");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.DiscordGuildId).IsUnique();
        builder.Property(x => x.DiscordGuildId).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(256).IsRequired();
        builder.Property(x => x.IconUrl).HasMaxLength(512);
        builder.Property(x => x.DisplayName).HasMaxLength(256);
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.CommunityType).HasMaxLength(100);
        builder.Property(x => x.SupportMessage).HasMaxLength(2000);
        builder.Property(x => x.RulesUrl).HasMaxLength(512);
        builder.Property(x => x.WebsiteUrl).HasMaxLength(512);
        builder.Property(x => x.OwnerDiscordUserId).HasMaxLength(32).IsRequired();

        builder.HasOne(x => x.Settings)
            .WithOne(x => x.Guild)
            .HasForeignKey<GuildSettings>(x => x.GuildId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Channels)
            .WithOne(x => x.Guild)
            .HasForeignKey(x => x.GuildId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Roles)
            .WithOne(x => x.Guild)
            .HasForeignKey(x => x.GuildId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Warnings)
            .WithOne(x => x.Guild)
            .HasForeignKey(x => x.GuildId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.ModerationCases)
            .WithOne(x => x.Guild)
            .HasForeignKey(x => x.GuildId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.GuildModules)
            .WithOne(x => x.Guild)
            .HasForeignKey(x => x.GuildId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.ReactionRoles)
            .WithOne(x => x.Guild)
            .HasForeignKey(x => x.GuildId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Subscription)
            .WithOne(x => x.Guild)
            .HasForeignKey<GuildSubscription>(x => x.GuildId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
