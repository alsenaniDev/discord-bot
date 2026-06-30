namespace DiscordBot.Infrastructure.Options;

/// <summary>
/// JWT signing settings. Secret must be long and random in production.
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "DiscordBot";
    public string Audience { get; set; } = "DiscordBot.Dashboard";
    public int ExpiresMinutes { get; set; } = 60;
}
