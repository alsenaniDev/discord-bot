using System.Net.Http.Json;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace DiscordBot.Bot.Services;

public sealed class DiscordActivityLaunchService(ILogger<DiscordActivityLaunchService> logger) : IDisposable
{
    // Kept outside IHttpClientFactory so standard HTTP logging never records the
    // interaction token embedded in Discord's callback URL.
    private readonly HttpClient _http = new() { BaseAddress = new Uri("https://discord.com/api/v10/"), Timeout = TimeSpan.FromMilliseconds(1200) };

    public async Task<ActivityLaunchResult> TryLaunchAsync(SocketInteraction interaction, CancellationToken ct = default)
    {
        try
        {
            var path = $"interactions/{interaction.Id}/{Uri.EscapeDataString(interaction.Token)}/callback";
            using var response = await _http.PostAsJsonAsync(path, new { type = 12 }, ct);
            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation("Discord Activity launched for interaction {InteractionId}, guild {GuildId}, channel {ChannelId}.", interaction.Id, interaction.GuildId, interaction.Channel.Id);
                return ActivityLaunchResult.Launched;
            }
            var error = await response.Content.ReadFromJsonAsync<DiscordCallbackError>(cancellationToken: ct).ConfigureAwait(false);
            logger.LogWarning("Discord Activity launch failed for interaction {InteractionId} with status {StatusCode}, Discord code {DiscordCode}: {DiscordMessage}.", interaction.Id, (int)response.StatusCode, error?.Code, error?.Message);
            return ActivityLaunchResult.Rejected;
        }
        catch (OperationCanceledException ex)
        {
            logger.LogWarning(ex, "Discord Activity launch outcome is unknown for interaction {InteractionId}; no second initial response will be attempted.", interaction.Id);
            return ActivityLaunchResult.Unknown;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Discord Activity launch outcome is unknown for interaction {InteractionId}; no second initial response will be attempted.", interaction.Id);
            return ActivityLaunchResult.Unknown;
        }
    }

    public void Dispose() => _http.Dispose();

    private sealed class DiscordCallbackError { public int? Code { get; set; } public string? Message { get; set; } }
}

public enum ActivityLaunchResult { Launched, Rejected, Unknown }
