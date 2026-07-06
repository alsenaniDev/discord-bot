namespace DiscordBot.Domain.Entities;

public class RouletteRoomPlayer : BaseEntity
{
    public Guid RouletteRoomId { get; set; }
    public RouletteRoom RouletteRoom { get; set; } = null!;
    public string UserDiscordId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public bool IsHost { get; set; }
    public bool IsAlive { get; set; } = true;
    public int Position { get; set; }
    public int Eliminations { get; set; }
    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? EliminatedAt { get; set; }
}
