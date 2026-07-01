namespace DiscordBot.Infrastructure.Options;

/// <summary>
/// Discord application settings from appsettings.json / environment variables.
/// </summary>
public class DiscordOptions
{
    public const string SectionName = "Discord";

    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string BotToken { get; set; } = string.Empty;

    /// <summary>API callback URL registered in the Discord Developer Portal.</summary>
    public string RedirectUri { get; set; } = string.Empty;

    /// <summary>Angular app URL(s) for CORS — comma-separated for multiple domains (e.g. Vercel + custom).</summary>
    public string DashboardUrl { get; set; } = "http://localhost:4200";
}
