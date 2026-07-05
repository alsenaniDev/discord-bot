namespace DiscordBot.Domain.Entities;

/// <summary>
/// A person who logged into the dashboard via Discord OAuth.
/// Discord snowflakes are stored as strings to avoid precision issues.
/// </summary>
public class User : BaseEntity
{
    public required string DiscordUserId { get; set; }
    public required string Username { get; set; }
    public string? GlobalName { get; set; }
    public string? AvatarUrl { get; set; }
    public string? DiscordAccessToken { get; set; }
    public string? DiscordRefreshToken { get; set; }
    public DateTimeOffset? DiscordTokenExpiresAtUtc { get; set; }
    public string? DiscordTokenScope { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
}
