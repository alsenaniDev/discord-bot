using DiscordBot.Activities.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiscordBot.Activities.Infrastructure.Data.Configurations;

public class ActivityPlayerConfiguration : IEntityTypeConfiguration<ActivityPlayer>
{
    public void Configure(EntityTypeBuilder<ActivityPlayer> builder)
    {
        builder.ToTable("ActivityPlayers");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.ActivitySessionId, x.DiscordUserId }).IsUnique();
        builder.Property(x => x.DiscordUserId).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Username).HasMaxLength(120).IsRequired();
        builder.Property(x => x.AvatarUrl).HasMaxLength(512);
        builder.Property(x => x.ConnectionStatus).HasMaxLength(24).IsRequired();
        builder.Property(x => x.LastConnectionId).HasMaxLength(128);
        builder.HasOne(x => x.ActivitySession).WithMany(x => x.Players).HasForeignKey(x => x.ActivitySessionId).OnDelete(DeleteBehavior.Cascade);
    }
}
