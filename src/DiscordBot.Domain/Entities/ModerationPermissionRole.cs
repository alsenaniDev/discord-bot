namespace DiscordBot.Domain.Entities;

/// <summary>
/// Discord role allowed to run bot moderation commands in a guild.
/// Separate from dashboard staff (<see cref="GuildPermissionRole"/>).
/// </summary>
public class ModerationPermissionRole : BaseEntity
{
    public Guid GuildId { get; set; }
    public Guild Guild { get; set; } = null!;

    public required string RoleDiscordId { get; set; }

    public bool CanWarn { get; set; }
    public bool CanViewWarnings { get; set; }
    public bool CanClearMessages { get; set; }
    public bool CanKick { get; set; }
    public bool CanViewModerationCases { get; set; }
    public bool CanViewLogs { get; set; }
}
