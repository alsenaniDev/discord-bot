using Discord;
using System.Net.Http.Json;
using System.Text.Json;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace DiscordBot.Bot.Services;

public sealed class DiscordActivityLaunchService(DiscordSocketClient discordClient, ILogger<DiscordActivityLaunchService> logger) : IDisposable
{
    // Kept outside IHttpClientFactory so standard HTTP logging never records the
    // interaction token embedded in Discord's callback URL.
    private readonly HttpClient _http = new() { BaseAddress = new Uri("https://discord.com/api/v10/"), Timeout = TimeSpan.FromMilliseconds(1200) };
    private volatile bool _availabilityLoaded;
    private volatile bool _isEmbedded;

    public async Task RefreshAvailabilityAsync()
    {
        try
        {
            var application = await discordClient.GetApplicationInfoAsync();
            _isEmbedded = application.Flags.HasFlag(ApplicationFlags.Embedded);
            _availabilityLoaded = true;
            logger.LogInformation(
                "Discord Activity availability checked for application {ApplicationId}. Embedded flag: {Embedded}; flags: {ApplicationFlags}.",
                application.Id,
                _isEmbedded,
                application.Flags);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not check whether the Discord application has the EMBEDDED flag.");
        }
    }

    public async Task<ActivityLaunchAttempt> TryLaunchAsync(SocketInteraction interaction, CancellationToken ct = default)
    {
        // Ready normally primes this value. If /games arrives before that check has
        // completed, wait for the result instead of incorrectly showing fallback on
        // the first invocation.
        if (!_availabilityLoaded)
        {
            logger.LogInformation(
                "Discord Activity availability has not loaded yet; checking application {ApplicationId} before launch.",
                discordClient.CurrentUser?.Id);
            await RefreshAvailabilityAsync();
        }

        if (!_availabilityLoaded)
        {
            logger.LogWarning(
                "Discord Activity launch failed before initial response. Application availability could not be loaded; showing fallback is safe.");
            return ActivityLaunchAttempt.SafeFallback;
        }

        if (!_isEmbedded)
        {
            logger.LogWarning(
                "Discord Activity launch failed before initial response. Application {ApplicationId} does not have the EMBEDDED flag; showing fallback is safe. Enable Activities for this exact application in Discord Developer Portal.",
                discordClient.CurrentUser?.Id);
            return ActivityLaunchAttempt.SafeFallback;
        }

        if (interaction.Id == 0 || string.IsNullOrWhiteSpace(interaction.Token))
        {
            logger.LogError("Discord Activity launch failed before initial response because interaction ID or token is missing.");
            return ActivityLaunchAttempt.SafeFallback;
        }

        logger.LogInformation(
            "Attempting to launch Discord Activity for interaction {InteractionId}, guild {GuildId}, channel {ChannelId}",
            interaction.Id,
            interaction.GuildId,
            interaction.Channel.Id);
        logger.LogInformation(
            "Sending LAUNCH_ACTIVITY callback to /interactions/{InteractionId}/{InteractionToken}/callback",
            interaction.Id,
            "{InteractionToken}");

        var callbackStarted = false;
        try
        {
            var path = $"interactions/{interaction.Id}/{Uri.EscapeDataString(interaction.Token)}/callback";
            callbackStarted = true;
            // Discord interaction callback payload must be exactly: { "type": 12 }.
            using var response = await _http.PostAsJsonAsync(path, new { type = 12 }, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);
            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation("Discord Activity launch callback accepted.");
                return ActivityLaunchAttempt.Accepted;
            }
            var safeBody = Sanitize(responseBody, interaction.Token);
            logger.LogWarning(
                "Discord rejected LAUNCH_ACTIVITY. Status {StatusCode}, Discord error code {DiscordCode}, response body: {ResponseBody}",
                (int)response.StatusCode,
                ReadDiscordCode(responseBody),
                safeBody);
            return ActivityLaunchAttempt.UnsafeFailure;
        }
        catch (OperationCanceledException ex)
        {
            logger.LogWarning(ex, "Discord Activity launch timed out for interaction {InteractionId}.", interaction.Id);
            return callbackStarted ? ActivityLaunchAttempt.UnsafeFailure : ActivityLaunchAttempt.SafeFallback;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Discord Activity launch request failed for interaction {InteractionId}.", interaction.Id);
            return callbackStarted ? ActivityLaunchAttempt.UnsafeFailure : ActivityLaunchAttempt.SafeFallback;
        }
    }

    public void Dispose() => _http.Dispose();

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
}

public readonly record struct ActivityLaunchAttempt(bool WasAccepted, bool CanFallback)
{
    public static ActivityLaunchAttempt Accepted => new(true, false);
    public static ActivityLaunchAttempt SafeFallback => new(false, true);
    public static ActivityLaunchAttempt UnsafeFailure => new(false, false);
}
