using DiscordBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiscordBot.Infrastructure.Data.Configurations;

public class GuildSubscriptionConfiguration : IEntityTypeConfiguration<GuildSubscription>
{
    public void Configure(EntityTypeBuilder<GuildSubscription> builder)
    {
        builder.ToTable("GuildSubscriptions");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.GuildId).IsUnique();

        builder.Property(x => x.Status).IsRequired();

        builder.HasOne(x => x.ApprovedRequest)
            .WithMany()
            .HasForeignKey(x => x.ApprovedRequestId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
