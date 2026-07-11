namespace DiscordBot.Activities.Api.Options;

public sealed class ActivitiesCorsOptions
{
    public const string SectionName = "Cors";
    public string[] AllowedOrigins { get; set; } = [];
}
