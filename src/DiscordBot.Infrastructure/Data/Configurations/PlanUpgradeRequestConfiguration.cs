using DiscordBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiscordBot.Infrastructure.Data.Configurations;

public class PlanUpgradeRequestConfiguration : IEntityTypeConfiguration<PlanUpgradeRequest>
{
    public void Configure(EntityTypeBuilder<PlanUpgradeRequest> builder)
    {
        builder.ToTable("PlanUpgradeRequests");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.DurationMonths).IsRequired();
        builder.Property(x => x.AdminNote).HasMaxLength(2000);

        builder.HasOne(x => x.Guild)
            .WithMany()
            .HasForeignKey(x => x.GuildId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.RequestedPlan)
            .WithMany()
            .HasForeignKey(x => x.RequestedPlanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CurrentPlan)
            .WithMany()
            .HasForeignKey(x => x.CurrentPlanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.RequestedByUser)
            .WithMany()
            .HasForeignKey(x => x.RequestedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ReviewedByAdmin)
            .WithMany()
            .HasForeignKey(x => x.ReviewedByAdminId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.GuildId, x.Status });
    }
}
