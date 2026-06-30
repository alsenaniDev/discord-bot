namespace DiscordBot.Domain.Entities;

/// <summary>
/// Per-guild enable/disable state for a platform module.
/// </summary>
public class GuildModule : BaseEntity
{
    public Guid GuildId { get; set; }
    public Guild Guild { get; set; } = null!;

    public Guid ModuleId { get; set; }
    public Module Module { get; set; } = null!;

    public bool IsEnabled { get; set; } = true;
}
