namespace DiscordBot.Infrastructure.Models;

public sealed class CommandPanelButtonDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Style { get; set; } = "Secondary";
    public bool Enabled { get; set; } = true;
    public int Order { get; set; }
}

public sealed class CommandPanelConfigDto
{
    public bool Enabled { get; init; }
    public string? ChannelId { get; init; }
    public string? MessageId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public IReadOnlyList<CommandPanelButtonDefinition> Buttons { get; init; } = [];
}

public sealed class CommandPanelRefreshDto
{
    public string DiscordGuildId { get; init; } = string.Empty;
    public CommandPanelConfigDto Config { get; init; } = null!;
}

public sealed class AckCommandPanelRequest
{
    public string? MessageId { get; set; }
}

public sealed class TicketChannelCleanupDto
{
    public Guid TicketId { get; init; }
    public string DiscordGuildId { get; init; } = string.Empty;
    public string ChannelDiscordId { get; init; } = string.Empty;
    public int TicketNumber { get; init; }
    public string OwnerDiscordUserId { get; init; } = string.Empty;
    public string? OwnerDisplayName { get; init; }
    public string? ClosedByDiscordUserId { get; init; }
    public string? ClosedByDisplayName { get; init; }
    public DateTimeOffset? ClosedAt { get; init; }
    public string? TicketArchiveChannelId { get; init; }
    public string TicketClosedFromDashboardMessage { get; init; } = string.Empty;
}
