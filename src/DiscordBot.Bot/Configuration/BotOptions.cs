namespace DiscordBot.Bot.Configuration;

public class BotOptions
{
    public const string SectionName = "Discord";

    public string Token { get; set; } = string.Empty;
}

public class ApiOptions
{
    public const string SectionName = "Api";

    public string BaseUrl { get; set; } = "https://localhost:5001";
    public string ApiKey { get; set; } = string.Empty;
}
