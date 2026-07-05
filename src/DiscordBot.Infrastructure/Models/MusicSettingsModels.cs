namespace DiscordBot.Infrastructure.Models;

public sealed class GuildMusicSettingsDto
{
    public Guid GuildId { get; init; }
    public bool IsEnabled { get; init; }
    public string? DjRoleDiscordId { get; init; }
    public int MaxQueueSize { get; init; }
    public int MaxTrackDurationSeconds { get; init; }
    public int DefaultVolume { get; init; }
    public bool AllowEveryoneToQueue { get; init; }
    public DateTimeOffset? CreatedAtUtc { get; init; }
    public DateTimeOffset? UpdatedAtUtc { get; init; }
}

public sealed class UpdateGuildMusicSettingsRequest
{
    public bool IsEnabled { get; set; }
    public string? DjRoleDiscordId { get; set; }
    public int MaxQueueSize { get; set; } = 50;
    public int MaxTrackDurationSeconds { get; set; } = 600;
    public int DefaultVolume { get; set; } = 50;
    public bool AllowEveryoneToQueue { get; set; } = true;
}
