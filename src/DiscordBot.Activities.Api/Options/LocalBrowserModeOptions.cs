namespace DiscordBot.Activities.Api.Options;

public sealed class LocalBrowserModeOptions
{
    public const string SectionName = "LocalBrowserMode";
    public bool Enabled { get; set; }
    public string GuildDiscordId { get; set; } = string.Empty;
    public string ChannelDiscordId { get; set; } = string.Empty;
    public string ActivityInstanceId { get; set; } = "local-browser-activity";
    public List<LocalBrowserProfileOptions> Profiles { get; set; } = [];
}

public sealed class LocalBrowserProfileOptions
{
    public string Name { get; set; } = string.Empty;
    public string DiscordUserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}
