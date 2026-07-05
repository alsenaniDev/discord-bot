namespace DiscordBot.Domain.Entities;

public class GuildMusicSettings : BaseEntity
{
    public Guid GuildId { get; set; }
    public Guild Guild { get; set; } = null!;
    public bool IsEnabled { get; set; }
    public string? DjRoleDiscordId { get; set; }
    public int MaxQueueSize { get; set; } = 50;
    public int MaxTrackDurationSeconds { get; set; } = 600;
    public int DefaultVolume { get; set; } = 50;
    public bool AllowEveryoneToQueue { get; set; } = true;
}
