namespace DiscordBot.Infrastructure.Auth;

/// <summary>
/// Minimal Discord profile returned after OAuth — used to upsert our User entity.
/// </summary>
public sealed class DiscordProfile
{
    public required string DiscordUserId { get; init; }
    public required string Username { get; init; }
    public string? GlobalName { get; init; }
    public string? AvatarUrl { get; init; }
    public string DiscordAccessToken { get; set; } = null!;
    public string DiscordRefreshToken { get; set; } = null!;
    public DateTime DiscordTokenExpiresAtUtc { get; set; }
    public string DiscordTokenScope { get; set; } = null!;
}
