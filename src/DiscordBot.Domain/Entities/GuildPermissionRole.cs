using DiscordBot.Domain.Enums;

namespace DiscordBot.Domain.Entities;

public class GuildPermissionRole : BaseEntity
{
    public Guid GuildId { get; set; }
    public Guild Guild { get; set; } = null!;

    public required string Name { get; set; }
    public required string DiscordRoleId { get; set; }
    public GuildPermissions Permissions { get; set; }
}
