namespace DiscordBot.Infrastructure.Options;

/// <summary>
/// Optional development seed data for testing guild endpoints.
/// Set OwnerDiscordUserId to your Discord user id (from /api/auth/me after login).
/// </summary>
public class SeedOptions
{
    public const string SectionName = "Seed";

    public bool Enabled { get; set; }
    public string OwnerDiscordUserId { get; set; } = string.Empty;
    public string DiscordGuildId { get; set; } = "123456789012345678";
    public string GuildName { get; set; } = "My Test Server";
}
