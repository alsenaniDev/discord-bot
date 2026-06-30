namespace DiscordBot.Infrastructure.Models;

public sealed class ReactionRoleDto
{
    public Guid Id { get; init; }
    public Guid GuildId { get; init; }
    public string ChannelDiscordId { get; init; } = string.Empty;
    public string MessageDiscordId { get; init; } = string.Empty;
    public string RoleDiscordId { get; init; } = string.Empty;
    public string ButtonCustomId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string ButtonLabel { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class CreateReactionRoleRequest
{
    public required string DiscordGuildId { get; set; }
    public required string ChannelDiscordId { get; set; }
    public required string MessageDiscordId { get; set; }
    public required string RoleDiscordId { get; set; }
    public required string ButtonCustomId { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required string ButtonLabel { get; set; }
    public string? CreatedByDiscordUserId { get; set; }
}
