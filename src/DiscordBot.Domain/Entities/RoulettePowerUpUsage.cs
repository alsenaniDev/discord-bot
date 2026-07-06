namespace DiscordBot.Domain.Entities;

public class RoulettePowerUpUsage : BaseEntity
{
    public Guid RouletteRoomId { get; set; }
    public RouletteRoom RouletteRoom { get; set; } = null!;
    public string UserDiscordId { get; set; } = string.Empty;
    public Guid GamePowerUpDefinitionId { get; set; }
    public GamePowerUpDefinition GamePowerUpDefinition { get; set; } = null!;
    public int RoundNumber { get; set; }
    public string ResultJson { get; set; } = "{}";
}
