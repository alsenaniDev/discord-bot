namespace DiscordBot.Domain.Entities;

public class Warning : BaseEntity
{
    public Guid GuildId { get; set; }
    public Guild Guild { get; set; } = null!;

    public string TargetDiscordUserId { get; set; } = string.Empty;
    public string ModeratorDiscordUserId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}
