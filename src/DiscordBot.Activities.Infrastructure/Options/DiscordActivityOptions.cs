namespace DiscordBot.Activities.Infrastructure.Options;

public sealed class DiscordActivityOptions
{
    public const string SectionName = "Discord";
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
}
