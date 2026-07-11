namespace DiscordBot.Activities.Infrastructure.Options;

public sealed class ActivitiesJwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = "DiscordBot.Activities";
    public string Audience { get; set; } = "DiscordBot.Activity";
    public string SigningKey { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 30;
}
