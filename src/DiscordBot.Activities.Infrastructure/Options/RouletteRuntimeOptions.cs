namespace DiscordBot.Activities.Infrastructure.Options;

public sealed class RouletteRuntimeOptions
{
    public const string SectionName = "Roulette";

    public int WaitingRoomExpirationMinutes { get; set; } = 60;
    public int InProgressAbandonmentMinutes { get; set; } = 180;
    public int ResumeWindowMinutes { get; set; } = 720;
    public int JoinIntentExpirationMinutes { get; set; } = 5;
    public int CleanupIntervalSeconds { get; set; } = 60;
}
