using DiscordBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiscordBot.Infrastructure.Data.Configurations;

public class GuildStaffConfiguration : IEntityTypeConfiguration<GuildStaff>
{
    public void Configure(EntityTypeBuilder<GuildStaff> builder)
    {
        builder.ToTable("GuildStaff");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DiscordUserId).HasMaxLength(32).IsRequired();
        builder.Property(x => x.CreatedByDiscordUserId).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Role).IsRequired();

        builder.HasOne(x => x.Guild)
            .WithMany()
            .HasForeignKey(x => x.GuildId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.GuildId, x.DiscordUserId }).IsUnique();
    }
}
