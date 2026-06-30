namespace DiscordBot.Infrastructure.Options;

/// <summary>
/// Platform admin seed configuration.
/// Set DiscordUserId to your Discord user id (from /api/auth/me after login).
/// </summary>
public class AdminOptions
{
    public const string SectionName = "Admin";

    public string DiscordUserId { get; set; } = string.Empty;
}
