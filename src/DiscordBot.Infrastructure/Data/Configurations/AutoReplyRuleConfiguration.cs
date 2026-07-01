using DiscordBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DiscordBot.Infrastructure.Data.Configurations;

public class AutoReplyRuleConfiguration : IEntityTypeConfiguration<AutoReplyRule>
{
    public void Configure(EntityTypeBuilder<AutoReplyRule> builder)
    {
        builder.ToTable("AutoReplyRules");

        builder.HasIndex(x => new { x.GuildId, x.Priority });
        builder.Property(x => x.Trigger).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Response).HasMaxLength(2000).IsRequired();
    }
}

public class TicketOutboundMessageConfiguration : IEntityTypeConfiguration<TicketOutboundMessage>
{
    public void Configure(EntityTypeBuilder<TicketOutboundMessage> builder)
    {
        builder.ToTable("TicketOutboundMessages");

        builder.HasIndex(x => new { x.GuildId, x.IsDelivered, x.CreatedAt });
        builder.Property(x => x.Content).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.SenderDiscordUserId).HasMaxLength(32).IsRequired();
        builder.Property(x => x.SenderDisplayName).HasMaxLength(128);
    }
}
