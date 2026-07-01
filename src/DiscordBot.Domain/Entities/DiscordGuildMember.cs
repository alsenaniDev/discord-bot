namespace DiscordBot.Domain.Entities;

public class DiscordGuildMember : BaseEntity
{
    public Guid GuildId { get; set; }
    public Guild Guild { get; set; } = null!;

    public string DiscordUserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? GlobalName { get; set; }
    public string? Nickname { get; set; }
    public string DiscordRoleIdsJson { get; set; } = "[]";
}
