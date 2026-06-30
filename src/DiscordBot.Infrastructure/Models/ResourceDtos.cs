using DiscordBot.Domain.Enums;

namespace DiscordBot.Infrastructure.Models;

public sealed class DiscordChannelDto
{
    public string DiscordChannelId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public DiscordChannelType Type { get; init; }
    public int Position { get; init; }
}

public sealed class DiscordRoleDto
{
    public string DiscordRoleId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int? Color { get; init; }
    public int Position { get; init; }
    public bool IsManaged { get; init; }
}

public sealed class SyncResourcesRequest
{
    public List<SyncChannelItem> Channels { get; set; } = [];
    public List<SyncRoleItem> Roles { get; set; } = [];
}

public sealed class SyncChannelItem
{
    public required string DiscordChannelId { get; set; }
    public required string Name { get; set; }
    public DiscordChannelType Type { get; set; }
    public int Position { get; set; }
}

public sealed class SyncRoleItem
{
    public required string DiscordRoleId { get; set; }
    public required string Name { get; set; }
    public int? Color { get; set; }
    public int Position { get; set; }
    public bool IsManaged { get; set; }
}

public sealed class RequestResourceSyncResponse
{
    public string Message { get; init; } = string.Empty;
    public DateTimeOffset? ResourcesSyncedAt { get; init; }
}
