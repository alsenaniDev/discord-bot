using System.Collections.Concurrent;

namespace DiscordBot.Bot.Music;

public sealed class MusicSessionManager
{
    private readonly ConcurrentDictionary<ulong, GuildMusicSession> _sessions = new();
    public GuildMusicSession? GetSession(ulong guildId) => _sessions.GetValueOrDefault(guildId);
    public bool HasSessionInDifferentVoiceChannel(ulong guildId, ulong voiceChannelId) => GetSession(guildId) is { } x && x.VoiceChannelId != voiceChannelId;
    public GuildMusicSession GetOrCreateSession(ulong guildId, ulong voiceChannelId, ulong textChannelId, int volume, out bool created)
    {
        var candidate = new GuildMusicSession { GuildId = guildId, VoiceChannelId = voiceChannelId, TextChannelId = textChannelId, Volume = volume };
        var value = _sessions.GetOrAdd(guildId, candidate); created = ReferenceEquals(value, candidate); return value;
    }
    public bool RemoveSession(ulong guildId, out GuildMusicSession? session) => _sessions.TryRemove(guildId, out session);
    public void EnqueueTrack(GuildMusicSession session, MusicTrack track) { lock (session.SyncRoot) { session.Queue.Enqueue(track); session.LastActivityUtc = DateTimeOffset.UtcNow; session.IdleSinceUtc = null; } }
    public void TrackStarted(ulong guildId, string providerTrackId)
    {
        var session = GetSession(guildId); if (session is null) return;
        lock (session.SyncRoot)
        {
            MusicTrack? selected = null; var keep = new Queue<MusicTrack>();
            while (session.Queue.TryDequeue(out var item)) { if (selected is null && item.ProviderTrackId == providerTrackId) selected = item; else keep.Enqueue(item); }
            while (keep.TryDequeue(out var item)) session.Queue.Enqueue(item);
            session.CurrentTrack = selected ?? session.CurrentTrack; session.IsPaused = false; session.IdleSinceUtc = null; session.LastActivityUtc = DateTimeOffset.UtcNow;
        }
    }
    public void TrackEnded(ulong guildId)
    {
        var session = GetSession(guildId); if (session is null) return;
        lock (session.SyncRoot) { session.CurrentTrack = null; session.IsPaused = false; session.LastActivityUtc = DateTimeOffset.UtcNow; if (session.Queue.Count == 0) session.IdleSinceUtc = DateTimeOffset.UtcNow; }
    }
    public int PendingCount(GuildMusicSession session) { lock (session.SyncRoot) return session.Queue.Count; }
    public void RemoveQueuedTrack(GuildMusicSession session, string providerTrackId)
    {
        lock (session.SyncRoot)
        {
            var keep = new Queue<MusicTrack>(); var removed = false;
            while (session.Queue.TryDequeue(out var item)) { if (!removed && item.ProviderTrackId == providerTrackId) removed = true; else keep.Enqueue(item); }
            while (keep.TryDequeue(out var item)) session.Queue.Enqueue(item);
        }
    }
    public MusicSessionSnapshot? Snapshot(ulong guildId)
    {
        var session = GetSession(guildId); if (session is null) return null;
        lock (session.SyncRoot) return new(session.GuildId, session.VoiceChannelId, session.TextChannelId, session.CurrentTrack, session.Queue.ToList(), session.Volume, session.IsPaused, session.IdleSinceUtc);
    }
    public IReadOnlyList<MusicSessionSnapshot> Snapshots() => _sessions.Keys.Select(Snapshot).Where(x => x is not null).Cast<MusicSessionSnapshot>().ToList();
}
