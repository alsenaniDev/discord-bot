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
        var normalized = MusicQueryNormalizer.Normalize(query, options.Value.SearchPrefix);
        logger.LogInformation(
            "Music query normalized. RawQuery: {RawQuery}; Identifier: {Identifier}; QueryType: {QueryType}.",
            query,
            normalized.Identifier,
            normalized.IsUrl ? "URL" : "TextSearch");

        try
        {
            var result = await audio.Tracks.LoadTracksAsync(normalized.Identifier, TrackSearchMode.None, cancellationToken: cancellationToken);
            if (!result.HasMatches) return [];
            var matches = result.Tracks.Length > 0 ? result.Tracks : result.Track is { } single ? [single] : [];
            return matches.Take(5).Select(track => new MusicTrack
            {
                Title = track.Title,
                Author = track.Author,
                Url = track.Uri?.ToString() ?? normalized.Identifier,
                Duration = track.IsLiveStream ? null : track.Duration,
                ProviderTrackId = track.ToString()
            }).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lavalink track search failed for identifier {Identifier}", normalized.Identifier);
            throw new AudioProviderUnavailableException("The audio service is unavailable.", ex);
        }
    }
}

internal readonly record struct NormalizedMusicQuery(string Identifier, bool IsUrl);

internal static class MusicQueryNormalizer
{
    public static NormalizedMusicQuery Normalize(string rawQuery, string configuredSearchPrefix)
    {
        var query = rawQuery.Trim();
        if (Uri.TryCreate(query, UriKind.Absolute, out var uri))
        {
            return new NormalizedMusicQuery(NormalizeUrl(uri, query), true);
        }

        var prefix = configuredSearchPrefix.Trim().TrimEnd(':');
        return new NormalizedMusicQuery(string.IsNullOrEmpty(prefix) ? query : $"{prefix}:{query}", false);
    }

    private static string NormalizeUrl(Uri uri, string originalUrl)
    {
        if (!IsYouTubeHost(uri.Host))
        {
            return originalUrl;
        }

        if (uri.Host.Equals("youtu.be", StringComparison.OrdinalIgnoreCase))
        {
            var videoId = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            return string.IsNullOrWhiteSpace(videoId)
                ? StripTrackingParameters(uri)
                : BuildCanonicalWatchUrl(Uri.UnescapeDataString(videoId));
        }

        if (uri.AbsolutePath.Equals("/watch", StringComparison.OrdinalIgnoreCase))
        {
            var videoId = GetQueryParameter(uri.Query, "v");
            return string.IsNullOrWhiteSpace(videoId)
                ? StripTrackingParameters(uri)
                : BuildCanonicalWatchUrl(videoId);
        }

        return StripTrackingParameters(uri);
    }

    private static bool IsYouTubeHost(string host) =>
        host.Equals("youtu.be", StringComparison.OrdinalIgnoreCase) ||
        host.Equals("youtube.com", StringComparison.OrdinalIgnoreCase) ||
        host.EndsWith(".youtube.com", StringComparison.OrdinalIgnoreCase);

    private static string BuildCanonicalWatchUrl(string videoId) =>
        $"https://www.youtube.com/watch?v={Uri.EscapeDataString(videoId)}";

    private static string? GetQueryParameter(string query, string name)
    {
        foreach (var pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            if (Uri.UnescapeDataString(parts[0]).Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return parts.Length == 2 ? Uri.UnescapeDataString(parts[1].Replace('+', ' ')) : string.Empty;
            }
        }

        return null;
    }

    private static string StripTrackingParameters(Uri uri)
    {
        var trackingParameters = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "si", "feature", "app", "utm_source", "utm_medium", "utm_campaign", "utm_content", "utm_term"
        };
        var retained = uri.Query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Where(pair => !trackingParameters.Contains(Uri.UnescapeDataString(pair.Split('=', 2)[0])))
            .ToArray();
        var builder = new UriBuilder(uri) { Query = string.Join('&', retained), Fragment = string.Empty };
        return builder.Uri.AbsoluteUri;
    }
}
