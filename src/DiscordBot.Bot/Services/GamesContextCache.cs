using System.Collections.Concurrent;
using DiscordBot.Bot.Api;
using DiscordBot.Bot.Api.Models;
using Microsoft.Extensions.Logging;

namespace DiscordBot.Bot.Services;

public sealed class GamesContextCache(BotApiClient api, ILogger<GamesContextCache> logger)
{
    private static readonly TimeSpan MaxAge = TimeSpan.FromMinutes(2);
    private readonly ConcurrentDictionary<ulong, CacheEntry> _entries = new();

    public bool TryGet(ulong guildId, out BotGamesContextApiResponse context)
    {
        if (_entries.TryGetValue(guildId, out var entry) && DateTimeOffset.UtcNow - entry.LoadedAt <= MaxAge)
        {
            context = entry.Context;
            return true;
        }
        context = null!;
        return false;
    }

    public async Task RefreshAsync(ulong guildId, CancellationToken ct = default)
    {
        try
        {
            var context = await api.GetGamesContextAsync(guildId.ToString(), ct);
            if (context is not null) _entries[guildId] = new CacheEntry(context, DateTimeOffset.UtcNow);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception ex) { logger.LogWarning(ex, "Could not refresh games context cache for guild {GuildId}.", guildId); }
    }

    private sealed record CacheEntry(BotGamesContextApiResponse Context, DateTimeOffset LoadedAt);
}
