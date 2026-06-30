using Discord.WebSocket;
using DiscordBot.Bot.Api;
using Microsoft.Extensions.Logging;

namespace DiscordBot.Bot.Services;

/// <summary>
/// Collects Discord guild resources and uploads them to the API.
/// </summary>
public class ResourceSyncService
{
    private readonly BotApiClient _apiClient;
    private readonly ILogger<ResourceSyncService> _logger;

    public ResourceSyncService(BotApiClient apiClient, ILogger<ResourceSyncService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<bool> SyncGuildAsync(SocketGuild guild, CancellationToken cancellationToken = default)
    {
        var payload = ResourceCollector.Collect(guild);
        var success = await _apiClient.SyncResourcesAsync(guild.Id.ToString(), payload, cancellationToken);

        if (success)
        {
            _logger.LogInformation(
                "Synced {ChannelCount} channels and {RoleCount} roles for guild {GuildName}.",
                payload.Channels.Count,
                payload.Roles.Count,
                guild.Name);
        }
        else
        {
            _logger.LogWarning("Failed to sync resources for guild {GuildId}.", guild.Id);
        }

        return success;
    }

    public async Task ProcessPendingSyncRequestsAsync(
        DiscordSocketClient client,
        CancellationToken cancellationToken = default)
    {
        var pendingGuildIds = await _apiClient.GetPendingSyncRequestsAsync(cancellationToken);
        if (pendingGuildIds.Count == 0)
        {
            return;
        }

        foreach (var discordGuildId in pendingGuildIds)
        {
            if (!ulong.TryParse(discordGuildId, out var guildId))
            {
                continue;
            }

            var guild = client.GetGuild(guildId);
            if (guild is null)
            {
                _logger.LogDebug(
                    "Pending sync for guild {GuildId}, but bot is not in that server.",
                    discordGuildId);
                continue;
            }

            await SyncGuildAsync(guild, cancellationToken);
        }
    }
}
