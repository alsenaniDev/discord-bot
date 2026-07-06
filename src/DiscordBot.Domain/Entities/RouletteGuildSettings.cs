namespace DiscordBot.Domain.Entities;

public class RouletteGuildSettings : BaseEntity
{
    public Guid GuildId { get; set; }
    public Guild Guild { get; set; } = null!;
    public int MinPlayers { get; set; } = 2;
    public int MaxPlayers { get; set; } = 6;
    public int WinnerCoins { get; set; } = 100;
    public int SecondPlaceCoins { get; set; } = 50;
    public int ParticipationCoins { get; set; } = 10;
    public int JoinWindowSeconds { get; set; } = 120;
    public int TurnSeconds { get; set; } = 30;
    public bool AnnounceRoomCreated { get; set; } = true;
    public bool AnnounceWinner { get; set; } = true;
}
