namespace DiscordBot.Activities.Domain.Entities;

public class GameResult : ActivitiesEntity
{
    public Guid GameSessionId { get; set; }
    public GameSession GameSession { get; set; } = null!;
    public string GameKey { get; set; } = string.Empty;
    public string DiscordUserId { get; set; } = string.Empty;
    public int Score { get; set; }
    public bool Won { get; set; }
    public int PointsAwarded { get; set; }
    public string ResultJson { get; set; } = "{}";
}
