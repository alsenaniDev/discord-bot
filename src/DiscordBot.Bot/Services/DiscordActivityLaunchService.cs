using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Discord;
using Discord.WebSocket;
using DiscordBot.Bot.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DiscordBot.Bot.Services;

public sealed class DiscordActivityLaunchService(
    DiscordSocketClient discordClient,
    IOptionsMonitor<DiscordActivityOptions> options,
    ILogger<DiscordActivityLaunchService> logger) : IDisposable
{
    private const string DiscordApiBaseUrl = "https://discord.com/api/v10/";

    // Kept outside IHttpClientFactory so standard HTTP logging never records the
    // interaction token embedded in Discord's callback URL.
    private readonly HttpClient _http = new() { BaseAddress = new Uri(DiscordApiBaseUrl), Timeout = TimeSpan.FromMilliseconds(1200) };
    private readonly SemaphoreSlim _availabilityLock = new(1, 1);
    private readonly SemaphoreSlim _launchLock = new(1, 1);
    private readonly ConcurrentDictionary<ulong, LaunchRecord> _interactions = new();
    private readonly ConcurrentDictionary<ulong, DateTimeOffset> _userLaunches = new();
    private readonly ConcurrentDictionary<ulong, DateTimeOffset> _guildLaunches = new();
    private readonly object _metricsLock = new();

    private volatile bool _availabilityLoaded;
    private volatile bool _isEmbedded;
    private DateTimeOffset _availabilityLoadedAtUtc = DateTimeOffset.MinValue;
    private ulong? _applicationId;

    private long _successfulLaunches;
    private long _failedLaunches;
    private long _rateLimitedLaunches;
    private double _averageLatencyMs;
    private DateTimeOffset? _lastSuccessfulLaunchUtc;
    private DateTimeOffset? _lastRateLimitUtc;
    private TimeSpan? _lastRetryAfter;

    public async Task RefreshAvailabilityAsync(bool force = false, CancellationToken ct = default)
    {
        var ttl = TimeSpan.FromSeconds(Math.Max(30, options.CurrentValue.AvailabilityCacheSeconds));
        if (!force && _availabilityLoaded && DateTimeOffset.UtcNow - _availabilityLoadedAtUtc < ttl)
        {
            logger.LogDebug(
                "Discord Activity availability cache hit for application {ApplicationId}. Embedded={Embedded}, age={AgeSeconds}s, ttl={TtlSeconds}s.",
                _applicationId,
                _isEmbedded,
                (int)(DateTimeOffset.UtcNow - _availabilityLoadedAtUtc).TotalSeconds,
                (int)ttl.TotalSeconds);
            return;
        }

        await _availabilityLock.WaitAsync(ct);
        try
        {
            if (!force && _availabilityLoaded && DateTimeOffset.UtcNow - _availabilityLoadedAtUtc < ttl)
            {
                return;
            }

            var started = Stopwatch.StartNew();
            var application = await discordClient.GetApplicationInfoAsync();
            started.Stop();
            _applicationId = application.Id;
            _isEmbedded = application.Flags.HasFlag(ApplicationFlags.Embedded);
            _availabilityLoaded = true;
            _availabilityLoadedAtUtc = DateTimeOffset.UtcNow;
            logger.LogInformation(
                "Discord Activity availability checked for application {ApplicationId}. Embedded flag: {Embedded}; flags: {ApplicationFlags}; durationMs={DurationMs}.",
                application.Id,
                _isEmbedded,
                application.Flags,
                started.ElapsedMilliseconds);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not check whether the Discord application has the EMBEDDED flag.");
        }
        finally
        {
            _availabilityLock.Release();
        }
    }

    public async Task<ActivityLaunchAttempt> TryLaunchAsync(SocketInteraction interaction, CancellationToken ct = default)
    {
        var correlationId = Guid.NewGuid().ToString("N");
        var requestStart = DateTimeOffset.UtcNow;
        var endpoint = $"/interactions/{interaction.Id}/[token]/callback";

        if (!_interactions.TryAdd(interaction.Id, new LaunchRecord(requestStart, correlationId)))
        {
            logger.LogWarning(
                "Duplicate Discord Activity launch ignored. InteractionId={InteractionId}, GuildId={GuildId}, ChannelId={ChannelId}, UserId={UserId}, CorrelationId={CorrelationId}.",
                interaction.Id,
                interaction.GuildId,
                interaction.Channel.Id,
                interaction.User.Id,
                correlationId);
            return ActivityLaunchAttempt.Duplicate;
        }

        CleanupOldInteractionRecords(requestStart);

        if (!TryAcquireCooldown(interaction, requestStart, out var cooldownMessage, out var retryAfter))
        {
            logger.LogWarning(
                "Discord Activity launch throttled before callback. InteractionId={InteractionId}, GuildId={GuildId}, ChannelId={ChannelId}, UserId={UserId}, RetryAfterMs={RetryAfterMs}, CorrelationId={CorrelationId}.",
                interaction.Id,
                interaction.GuildId,
                interaction.Channel.Id,
                interaction.User.Id,
                retryAfter?.TotalMilliseconds,
                correlationId);
            return ActivityLaunchAttempt.Throttled(cooldownMessage);
        }

        if (!_availabilityLoaded)
        {
            logger.LogInformation(
                "Discord Activity availability has not loaded yet; checking application {ApplicationId} before launch. InteractionId={InteractionId}, CorrelationId={CorrelationId}.",
                discordClient.CurrentUser?.Id,
                interaction.Id,
                correlationId);
            await RefreshAvailabilityAsync(ct: ct);
        }
        else
        {
            await RefreshAvailabilityAsync(ct: ct);
        }

        if (!_availabilityLoaded)
        {
            logger.LogWarning(
                "Discord Activity launch failed before initial response. Application availability could not be loaded; showing fallback is safe. InteractionId={InteractionId}, CorrelationId={CorrelationId}.",
                interaction.Id,
                correlationId);
            RecordFailure(null);
            return ActivityLaunchAttempt.SafeFallback;
        }

        if (!_isEmbedded)
        {
            logger.LogWarning(
                "Discord Activity launch failed before initial response. Application {ApplicationId} does not have the EMBEDDED flag; showing fallback is safe. InteractionId={InteractionId}, CorrelationId={CorrelationId}.",
                _applicationId ?? discordClient.CurrentUser?.Id,
                interaction.Id,
                correlationId);
            RecordFailure(null);
            return ActivityLaunchAttempt.SafeFallback;
        }

        if (interaction.Id == 0 || string.IsNullOrWhiteSpace(interaction.Token))
        {
            logger.LogError("Discord Activity launch failed before initial response because interaction ID or token is missing. CorrelationId={CorrelationId}.", correlationId);
            RecordFailure(null);
            return ActivityLaunchAttempt.SafeFallback;
        }

        if (!await _launchLock.WaitAsync(TimeSpan.Zero, ct))
        {
            logger.LogWarning(
                "Discord Activity launch throttled because another launch callback is in flight. InteractionId={InteractionId}, GuildId={GuildId}, ChannelId={ChannelId}, UserId={UserId}, CorrelationId={CorrelationId}.",
                interaction.Id,
                interaction.GuildId,
                interaction.Channel.Id,
                interaction.User.Id,
                correlationId);
            return ActivityLaunchAttempt.Throttled("يتم فتح مركز ألعاب آخر الآن. حاول مرة ثانية بعد لحظات.");
        }

        var callbackStarted = false;
        var stopwatch = Stopwatch.StartNew();
        try
        {
            logger.LogInformation(
                "Attempting to launch Discord Activity. InteractionId={InteractionId}, GuildId={GuildId}, ChannelId={ChannelId}, UserId={UserId}, ActivityId={ActivityId}, ApplicationId={ApplicationId}, RequestStart={RequestStart:o}, Endpoint={Endpoint}, CorrelationId={CorrelationId}.",
                interaction.Id,
                interaction.GuildId,
                interaction.Channel.Id,
                interaction.User.Id,
                _applicationId,
                _applicationId,
                requestStart,
                endpoint,
                correlationId);

            var path = $"interactions/{interaction.Id}/{Uri.EscapeDataString(interaction.Token)}/callback";
            callbackStarted = true;
            logger.LogInformation(
                "Sending LAUNCH_ACTIVITY callback to /interactions/{InteractionId}/{InteractionToken}/callback. CorrelationId={CorrelationId}.",
                interaction.Id,
                "{InteractionToken}",
                correlationId);

            // Discord interaction callback payload must be exactly: { "type": 12 }.
            using var request = new HttpRequestMessage(HttpMethod.Post, path)
            {
                Content = JsonContent.Create(new { type = 12 })
            };
            using var response = await _http.SendAsync(request, ct);
            stopwatch.Stop();
            var responseBody = await response.Content.ReadAsStringAsync(ct);
            var discordCode = ReadDiscordCode(responseBody);
            var retryAfterValue = GetRetryAfter(response, responseBody);
            var bucketId = TryGetHeader(response, "X-RateLimit-Bucket");

            if (response.IsSuccessStatusCode)
            {
                RecordSuccess(stopwatch.Elapsed);
                logger.LogInformation(
                    "Discord Activity launch callback accepted. InteractionId={InteractionId}, GuildId={GuildId}, ChannelId={ChannelId}, UserId={UserId}, ActivityId={ActivityId}, ApplicationId={ApplicationId}, RequestStart={RequestStart:o}, RequestEnd={RequestEnd:o}, DurationMs={DurationMs}, HttpStatus={StatusCode}, Success={Success}, CorrelationId={CorrelationId}.",
                    interaction.Id,
                    interaction.GuildId,
                    interaction.Channel.Id,
                    interaction.User.Id,
                    _applicationId,
                    _applicationId,
                    requestStart,
                    DateTimeOffset.UtcNow,
                    stopwatch.ElapsedMilliseconds,
                    (int)response.StatusCode,
                    true,
                    correlationId);
                return ActivityLaunchAttempt.Accepted;
            }

            var safeBody = Sanitize(responseBody, interaction.Token);
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                RecordRateLimit(stopwatch.Elapsed, retryAfterValue);
                logger.LogWarning(
                    "Discord rate limited LAUNCH_ACTIVITY. InteractionId={InteractionId}, GuildId={GuildId}, ChannelId={ChannelId}, UserId={UserId}, ActivityId={ActivityId}, ApplicationId={ApplicationId}, RequestStart={RequestStart:o}, RequestEnd={RequestEnd:o}, DurationMs={DurationMs}, HttpStatus={StatusCode}, DiscordCode={DiscordCode}, RetryAfterMs={RetryAfterMs}, Endpoint={Endpoint}, BucketId={BucketId}, ResponseBody={ResponseBody}, Success={Success}, CorrelationId={CorrelationId}.",
                    interaction.Id,
                    interaction.GuildId,
                    interaction.Channel.Id,
                    interaction.User.Id,
                    _applicationId,
                    _applicationId,
                    requestStart,
                    DateTimeOffset.UtcNow,
                    stopwatch.ElapsedMilliseconds,
                    (int)response.StatusCode,
                    discordCode,
                    retryAfterValue?.TotalMilliseconds,
                    endpoint,
                    bucketId,
                    safeBody,
                    false,
                    correlationId);
                return ActivityLaunchAttempt.RateLimited;
            }

            RecordFailure(stopwatch.Elapsed);
            logger.LogWarning(
                "Discord rejected LAUNCH_ACTIVITY. InteractionId={InteractionId}, GuildId={GuildId}, ChannelId={ChannelId}, UserId={UserId}, ActivityId={ActivityId}, ApplicationId={ApplicationId}, RequestStart={RequestStart:o}, RequestEnd={RequestEnd:o}, DurationMs={DurationMs}, HttpStatus={StatusCode}, DiscordCode={DiscordCode}, RetryAfterMs={RetryAfterMs}, Endpoint={Endpoint}, BucketId={BucketId}, ResponseBody={ResponseBody}, Success={Success}, CorrelationId={CorrelationId}.",
                interaction.Id,
                interaction.GuildId,
                interaction.Channel.Id,
                interaction.User.Id,
                _applicationId,
                _applicationId,
                requestStart,
                DateTimeOffset.UtcNow,
                stopwatch.ElapsedMilliseconds,
                (int)response.StatusCode,
                discordCode,
                retryAfterValue?.TotalMilliseconds,
                endpoint,
                bucketId,
                safeBody,
                false,
                correlationId);
            return ActivityLaunchAttempt.UnsafeFailure;
        }
        catch (OperationCanceledException ex) when (!ct.IsCancellationRequested)
        {
            stopwatch.Stop();
            RecordFailure(stopwatch.Elapsed);
            logger.LogWarning(ex, "Discord Activity launch timed out. InteractionId={InteractionId}, GuildId={GuildId}, ChannelId={ChannelId}, UserId={UserId}, DurationMs={DurationMs}, CorrelationId={CorrelationId}.", interaction.Id, interaction.GuildId, interaction.Channel.Id, interaction.User.Id, stopwatch.ElapsedMilliseconds, correlationId);
            return callbackStarted ? ActivityLaunchAttempt.UnsafeFailure : ActivityLaunchAttempt.SafeFallback;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            RecordFailure(stopwatch.Elapsed);
            logger.LogWarning(ex, "Discord Activity launch request failed. InteractionId={InteractionId}, GuildId={GuildId}, ChannelId={ChannelId}, UserId={UserId}, DurationMs={DurationMs}, CorrelationId={CorrelationId}.", interaction.Id, interaction.GuildId, interaction.Channel.Id, interaction.User.Id, stopwatch.ElapsedMilliseconds, correlationId);
            return callbackStarted ? ActivityLaunchAttempt.UnsafeFailure : ActivityLaunchAttempt.SafeFallback;
        }
        finally
        {
            _launchLock.Release();
        }
    }

    public ActivityLaunchDiagnostics GetDiagnostics()
    {
        lock (_metricsLock)
        {
            return new ActivityLaunchDiagnostics(
                Interlocked.Read(ref _successfulLaunches),
                Interlocked.Read(ref _failedLaunches),
                Interlocked.Read(ref _rateLimitedLaunches),
                _averageLatencyMs,
                _lastSuccessfulLaunchUtc,
                _lastRateLimitUtc,
                _lastRetryAfter,
                _launchLock.CurrentCount == 0,
                _userLaunches.Count,
                _guildLaunches.Count,
                _availabilityLoaded,
                _isEmbedded,
                _applicationId,
                _availabilityLoadedAtUtc,
                Math.Max(30, options.CurrentValue.AvailabilityCacheSeconds));
        }
    }

    public void Dispose()
    {
        _http.Dispose();
        _availabilityLock.Dispose();
        _launchLock.Dispose();
    }

    private bool TryAcquireCooldown(SocketInteraction interaction, DateTimeOffset now, out string message, out TimeSpan? retryAfter)
    {
        var userCooldown = TimeSpan.FromSeconds(Math.Max(1, options.CurrentValue.PerUserLaunchCooldownSeconds));
        var guildCooldown = TimeSpan.FromSeconds(Math.Max(1, options.CurrentValue.PerGuildLaunchCooldownSeconds));

        if (_userLaunches.TryGetValue(interaction.User.Id, out var lastUserLaunch) && now - lastUserLaunch < userCooldown)
        {
            retryAfter = userCooldown - (now - lastUserLaunch);
            message = "فتحت مركز الألعاب قبل لحظات. حاول مرة ثانية بعد ثوانٍ قليلة.";
            return false;
        }

        if (interaction.GuildId is { } guildId
            && _guildLaunches.TryGetValue(guildId, out var lastGuildLaunch)
            && now - lastGuildLaunch < guildCooldown)
        {
            retryAfter = guildCooldown - (now - lastGuildLaunch);
            message = "يوجد طلب فتح لمركز الألعاب في هذا السيرفر الآن. حاول مرة ثانية بعد لحظات.";
            return false;
        }

        _userLaunches[interaction.User.Id] = now;
        if (interaction.GuildId is { } guild) _guildLaunches[guild] = now;
        retryAfter = null;
        message = string.Empty;
        CleanupCooldowns(now, userCooldown, guildCooldown);
        return true;
    }

    private void CleanupCooldowns(DateTimeOffset now, TimeSpan userCooldown, TimeSpan guildCooldown)
    {
        foreach (var (key, value) in _userLaunches)
        {
            if (now - value > userCooldown + TimeSpan.FromSeconds(30)) _userLaunches.TryRemove(key, out _);
        }

        foreach (var (key, value) in _guildLaunches)
        {
            if (now - value > guildCooldown + TimeSpan.FromSeconds(30)) _guildLaunches.TryRemove(key, out _);
        }
    }

    private void CleanupOldInteractionRecords(DateTimeOffset now)
    {
        foreach (var (key, value) in _interactions)
        {
            if (now - value.CreatedAtUtc > TimeSpan.FromMinutes(15)) _interactions.TryRemove(key, out _);
        }
    }

    private void RecordSuccess(TimeSpan latency)
    {
        Interlocked.Increment(ref _successfulLaunches);
        lock (_metricsLock)
        {
            _lastSuccessfulLaunchUtc = DateTimeOffset.UtcNow;
            _averageLatencyMs = _averageLatencyMs <= 0 ? latency.TotalMilliseconds : (_averageLatencyMs * 0.85) + (latency.TotalMilliseconds * 0.15);
        }
    }

    private void RecordFailure(TimeSpan? latency)
    {
        Interlocked.Increment(ref _failedLaunches);
        if (latency.HasValue)
        {
            lock (_metricsLock)
            {
                _averageLatencyMs = _averageLatencyMs <= 0 ? latency.Value.TotalMilliseconds : (_averageLatencyMs * 0.85) + (latency.Value.TotalMilliseconds * 0.15);
            }
        }
    }

    private void RecordRateLimit(TimeSpan latency, TimeSpan? retryAfter)
    {
        Interlocked.Increment(ref _failedLaunches);
        Interlocked.Increment(ref _rateLimitedLaunches);
        lock (_metricsLock)
        {
            _lastRateLimitUtc = DateTimeOffset.UtcNow;
            _lastRetryAfter = retryAfter;
            _averageLatencyMs = _averageLatencyMs <= 0 ? latency.TotalMilliseconds : (_averageLatencyMs * 0.85) + (latency.TotalMilliseconds * 0.15);
        }
    }

    private static TimeSpan? GetRetryAfter(HttpResponseMessage response, string body)
    {
        if (response.Headers.RetryAfter?.Delta is { } delta) return delta;
        if (response.Headers.RetryAfter?.Date is { } date) return date - DateTimeOffset.UtcNow;
        if (TryGetHeader(response, "Retry-After") is { } retryAfterHeader && double.TryParse(retryAfterHeader, out var seconds)) return TimeSpan.FromSeconds(seconds);
        try
        {
            using var json = JsonDocument.Parse(body);
            if (json.RootElement.TryGetProperty("retry_after", out var retryAfter) && retryAfter.TryGetDouble(out var value)) return TimeSpan.FromSeconds(value);
        }
        catch (JsonException) { }
        return null;
    }

    private static string? TryGetHeader(HttpResponseMessage response, string headerName)
    {
        return response.Headers.TryGetValues(headerName, out var values) ? values.FirstOrDefault() : null;
    }

    private static int? ReadDiscordCode(string body)
    {
        try
        {
            using var json = JsonDocument.Parse(body);
            return json.RootElement.TryGetProperty("code", out var code) && code.TryGetInt32(out var value) ? value : null;
        }
        catch (JsonException) { return null; }
    }

    private static string Sanitize(string body, string token)
    {
        var safe = body.Replace(token, "[REDACTED]", StringComparison.Ordinal);
        return safe.Length <= 4000 ? safe : safe[..4000] + "…";
    }

    private sealed record LaunchRecord(DateTimeOffset CreatedAtUtc, string CorrelationId);
}

public readonly record struct ActivityLaunchAttempt(bool WasAccepted, bool CanFallback, bool IsThrottled, bool IsDuplicate, bool IsRateLimited, string? UserMessage)
{
    public static ActivityLaunchAttempt Accepted => new(true, false, false, false, false, null);
    public static ActivityLaunchAttempt SafeFallback => new(false, true, false, false, false, null);
    public static ActivityLaunchAttempt UnsafeFailure => new(false, false, false, false, false, null);
    public static ActivityLaunchAttempt Duplicate => new(false, false, false, true, false, null);
    public static ActivityLaunchAttempt RateLimited => new(false, false, false, false, true, "Discord يقيّد فتح واجهة الألعاب مؤقتًا. حاول مرة ثانية بعد قليل.");
    public static ActivityLaunchAttempt Throttled(string message) => new(false, true, true, false, false, message);
}

public sealed record ActivityLaunchDiagnostics(
    long SuccessfulLaunches,
    long FailedLaunches,
    long RateLimitedLaunches,
    double AverageLatencyMs,
    DateTimeOffset? LastSuccessfulLaunchUtc,
    DateTimeOffset? LastRateLimitUtc,
    TimeSpan? LastRetryAfter,
    bool LaunchInFlight,
    int UserCooldownCount,
    int GuildCooldownCount,
    bool AvailabilityLoaded,
    bool IsEmbedded,
    ulong? ApplicationId,
    DateTimeOffset AvailabilityLoadedAtUtc,
    int AvailabilityCacheSeconds);
