namespace DiscordBot.Bot.Configuration;

public sealed class LavalinkOptions
{
    public const string SectionName = "Lavalink";
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 2333;
    public string Password { get; set; } = "youshallnotpass";
    public bool Secure { get; set; }
    public string SearchPrefix { get; set; } = "ytsearch";
    public int IdleTimeoutSeconds { get; set; } = 30;
}
