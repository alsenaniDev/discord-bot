using System.Net.Http.Json;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace DiscordBot.Bot.Services;

public sealed class DiscordActivityLaunchService(ILogger<DiscordActivityLaunchService> logger) : IDisposable
{
    // Kept outside IHttpClientFactory so standard HTTP logging never records the
    // interaction token embedded in Discord's callback URL.
    private readonly HttpClient _http = new() { BaseAddress = new Uri("https://discord.com/api/v10/"), Timeout = TimeSpan.FromMilliseconds(1200) };

    public async Task<bool> TryLaunchAsync(SocketInteraction interaction, CancellationToken ct = default)
    {
        try
        {
            var path = $"interactions/{interaction.Id}/{Uri.EscapeDataString(interaction.Token)}/callback";
            using var response = await _http.PostAsJsonAsync(path, new { type = 12 }, ct);
            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation("Discord Activity launched for interaction {InteractionId}, guild {GuildId}, channel {ChannelId}.", interaction.Id, interaction.GuildId, interaction.Channel.Id);
                return true;
            }
            logger.LogWarning("Discord Activity launch failed for interaction {InteractionId} with status {StatusCode}.", interaction.Id, (int)response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Discord Activity launch request failed for interaction {InteractionId}.", interaction.Id);
            return false;
        }
    }

    public void Dispose() => _http.Dispose();
}
