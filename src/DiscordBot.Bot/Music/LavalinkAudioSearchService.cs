using DiscordBot.Bot.Configuration;
using Lavalink4NET;
using Lavalink4NET.Rest.Entities.Tracks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DiscordBot.Bot.Music;

public interface IAudioSearchService
{
    Task<IReadOnlyList<MusicTrack>> SearchAsync(string query, CancellationToken cancellationToken);
}

public sealed class AudioProviderUnavailableException(string message, Exception? inner = null) : Exception(message, inner);

public sealed class LavalinkAudioSearchService(IAudioService audio, IOptions<LavalinkOptions> options, ILogger<LavalinkAudioSearchService> logger) : IAudioSearchService
{
    public async Task<IReadOnlyList<MusicTrack>> SearchAsync(string query, CancellationToken cancellationToken)
    {
        try
        {
            var searchMode = Uri.TryCreate(query, UriKind.Absolute, out _) ? TrackSearchMode.None : new TrackSearchMode(options.Value.SearchPrefix);
            var result = await audio.Tracks.LoadTracksAsync(query, searchMode, cancellationToken: cancellationToken);
            if (!result.HasMatches) return [];
            var matches = result.Tracks.Length > 0 ? result.Tracks : result.Track is { } single ? [single] : [];
            return matches.Take(5).Select(track => new MusicTrack
            {
                Title = track.Title,
                Author = track.Author,
                Url = track.Uri?.ToString() ?? query,
                Duration = track.IsLiveStream ? null : track.Duration,
                ProviderTrackId = track.ToString()
            }).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lavalink track search failed for query {Query}", query);
            throw new AudioProviderUnavailableException("The audio service is unavailable.", ex);
        }
    }
}
