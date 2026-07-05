namespace DiscordBot.Bot.Music;

public sealed class MusicTrack
{
    public required string Title { get; init; }
    public required string Url { get; init; }
    public string? Author { get; init; }
    public TimeSpan? Duration { get; init; }
    public string RequestedByUsername { get; set; } = string.Empty;
    public ulong RequestedByUserId { get; set; }
    internal string ProviderTrackId { get; init; } = string.Empty;
}

public sealed class GuildMusicSession
{
    public ulong GuildId { get; init; }
    public ulong VoiceChannelId { get; init; }
    public ulong TextChannelId { get; set; }
    public MusicTrack? CurrentTrack { get; set; }
    public Queue<MusicTrack> Queue { get; } = new();
    public int Volume { get; set; }
    public bool IsPaused { get; set; }
    public DateTimeOffset LastActivityUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? IdleSinceUtc { get; set; }
    internal object SyncRoot { get; } = new();
    internal SemaphoreSlim OperationGate { get; } = new(1, 1);
}

public sealed record MusicSessionSnapshot(ulong GuildId, ulong VoiceChannelId, ulong TextChannelId, MusicTrack? CurrentTrack, IReadOnlyList<MusicTrack> Queue, int Volume, bool IsPaused, DateTimeOffset? IdleSinceUtc);
