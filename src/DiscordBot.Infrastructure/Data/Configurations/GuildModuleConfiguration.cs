using DiscordBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiscordBot.Infrastructure.Data.Configurations;

public class GuildModuleConfiguration : IEntityTypeConfiguration<GuildModule>
{
    public void Configure(EntityTypeBuilder<GuildModule> builder)
    {
        builder.ToTable("GuildModules");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.GuildId, x.ModuleId }).IsUnique();
    }
}
