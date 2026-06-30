namespace DiscordBot.Infrastructure.Options;

/// <summary>
/// Shared secret the Discord bot uses to call internal API endpoints.
/// </summary>
public class BotOptions
{
    public const string SectionName = "Bot";

    public string ApiKey { get; set; } = string.Empty;
}
