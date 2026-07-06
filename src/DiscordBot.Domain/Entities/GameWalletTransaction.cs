namespace DiscordBot.Domain.Entities;

public class GameWalletTransaction : BaseEntity
{
    public Guid GuildId { get; set; }
    public Guild Guild { get; set; } = null!;
    public string UserDiscordId { get; set; } = string.Empty;
    public int Amount { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public Guid? ReferenceId { get; set; }
}
