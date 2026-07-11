namespace DiscordBot.Activities.Infrastructure.Options;

public sealed class PlatformApiOptions
{
    public const string SectionName = "PlatformApi";
    public string BaseUrl { get; set; } = string.Empty;
    public string ServiceToken { get; set; } = string.Empty;
}
