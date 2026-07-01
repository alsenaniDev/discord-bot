using DiscordBot.Domain.Enums;

namespace DiscordBot.Infrastructure.Models;

public sealed class TicketDto
{
    public Guid Id { get; init; }
    public Guid GuildId { get; init; }
    public int TicketNumber { get; init; }
    public string OwnerDiscordUserId { get; init; } = string.Empty;
    public string ChannelDiscordId { get; init; } = string.Empty;
    public string? OwnerDisplayName { get; init; }
    public string? ChannelName { get; init; }
    public TicketStatus Status { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? ClosedAt { get; init; }
}

public sealed class CreateTicketRequest
{
    public required string DiscordGuildId { get; set; }
    public required string OwnerDiscordUserId { get; set; }
    public required string ChannelDiscordId { get; set; }
    public string? OwnerDisplayName { get; set; }
    public string? ChannelDisplayName { get; set; }
}

public sealed class SetupTicketsRequest
{
    public required string TicketCategoryId { get; set; }
}

public sealed class CloseTicketRequest
{
    public string? ClosedByDiscordUserId { get; set; }
    public string? ClosedByDisplayName { get; set; }
    public string? ChannelDisplayName { get; set; }
}
