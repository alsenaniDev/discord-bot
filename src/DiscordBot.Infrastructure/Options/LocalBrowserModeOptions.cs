namespace DiscordBot.Infrastructure.Options;

public sealed class LocalBrowserModeOptions
{
    public const string SectionName = "LocalBrowserMode";

    public bool Enabled { get; set; }
    public string GuildDiscordId { get; set; } = string.Empty;
    public string ChannelDiscordId { get; set; } = string.Empty;
    public string ActivityInstanceId { get; set; } = string.Empty;
    public LocalBrowserJwtOptions ActivitiesJwt { get; set; } = new();
    public List<LocalBrowserProfileOptions> Profiles { get; set; } = [];
}

public sealed class LocalBrowserJwtOptions
{
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string SigningKey { get; set; } = string.Empty;
}

public sealed class LocalBrowserProfileOptions
{
    public string Name { get; set; } = string.Empty;
    public string DiscordUserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}
