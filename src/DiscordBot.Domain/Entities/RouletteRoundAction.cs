namespace DiscordBot.Domain.Entities;

public class RouletteRoundAction : BaseEntity
{
    public Guid RouletteRoomId { get; set; }
    public RouletteRoom RouletteRoom { get; set; } = null!;
    public int RoundNumber { get; set; }
    public string ActorUserDiscordId { get; set; } = string.Empty;
    public string? TargetUserDiscordId { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string DataJson { get; set; } = "{}";
}
