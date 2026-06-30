using DiscordBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiscordBot.Infrastructure.Data.Configurations;

public class PlatformAdminConfiguration : IEntityTypeConfiguration<PlatformAdmin>
{
    public void Configure(EntityTypeBuilder<PlatformAdmin> builder)
    {
        builder.ToTable("PlatformAdmins");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.DiscordUserId).IsUnique();
        builder.Property(x => x.DiscordUserId).HasMaxLength(32);
    }
}
