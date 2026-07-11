namespace DiscordBot.Bot.Configuration;

public class PlatformOptions
{
    public const string SectionName = "Platform";

    public string DashboardUrl { get; set; } = "http://localhost:4200";
}

public class ActivitiesApiOptions
{
    public const string SectionName = "ActivitiesApi";

    public string BaseUrl { get; set; } = "https://localhost:7001";
    public string ServiceToken { get; set; } = string.Empty;
}
