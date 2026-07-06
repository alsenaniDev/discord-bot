namespace DiscordBot.Domain.Entities;

public class GameWallet : BaseEntity
{
    public Guid GuildId { get; set; }
    public Guild Guild { get; set; } = null!;
    public string UserDiscordId { get; set; } = string.Empty;
    public int Balance { get; set; }
}
