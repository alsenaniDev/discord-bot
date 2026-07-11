namespace DiscordBot.Bot.Configuration;

public sealed class DiscordActivityOptions
{
    public const string SectionName = "Activity";
    public bool Enabled { get; set; } = true;
    public int AvailabilityCacheSeconds { get; set; } = 600;
    public int PerUserLaunchCooldownSeconds { get; set; } = 5;
    public int PerGuildLaunchCooldownSeconds { get; set; } = 2;
}
