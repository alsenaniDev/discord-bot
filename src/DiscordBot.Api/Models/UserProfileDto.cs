namespace DiscordBot.Api.Models;

public sealed class UserProfileDto
{
    public Guid Id { get; init; }
    public string DiscordUserId { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string? GlobalName { get; init; }
    public string? AvatarUrl { get; init; }
    public DateTimeOffset? LastLoginAt { get; init; }
    public bool IsAdmin { get; init; }
}
