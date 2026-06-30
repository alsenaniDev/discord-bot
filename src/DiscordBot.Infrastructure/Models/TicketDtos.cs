using DiscordBot.Domain.Enums;

namespace DiscordBot.Infrastructure.Models;

public sealed class TicketDto
{
    public Guid Id { get; init; }
    public Guid GuildId { get; init; }
    public int TicketNumber { get; init; }
    public string OwnerDiscordUserId { get; init; } = string.Empty;
    public string ChannelDiscordId { get; init; } = string.Empty;
    public TicketStatus Status { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? ClosedAt { get; init; }
}

public sealed class CreateTicketRequest
{
    public required string DiscordGuildId { get; set; }
    public required string OwnerDiscordUserId { get; set; }
    public required string ChannelDiscordId { get; set; }
}

public sealed class SetupTicketsRequest
{
    public required string TicketCategoryId { get; set; }
}
