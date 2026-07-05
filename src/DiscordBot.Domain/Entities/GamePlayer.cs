namespace DiscordBot.Domain.Entities;

public class GamePlayer : BaseEntity
{
    public Guid GuildId { get; set; }
    public Guild Guild { get; set; } = null!;
    public string UserDiscordId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public int TotalPoints { get; set; }
    public int GamesPlayed { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int CurrentStreak { get; set; }
    public int BestStreak { get; set; }
    public DateTimeOffset? LastPlayedAt { get; set; }
}
