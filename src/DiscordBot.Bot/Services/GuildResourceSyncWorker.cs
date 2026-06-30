using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DiscordBot.Bot.Services;

/// <summary>
/// Polls the API for dashboard-requested resource syncs.
/// </summary>
public class GuildResourceSyncWorker : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);

    private readonly DiscordSocketClient _client;
    private readonly ResourceSyncService _syncService;
    private readonly ILogger<GuildResourceSyncWorker> _logger;

    public GuildResourceSyncWorker(
        DiscordSocketClient client,
        ResourceSyncService syncService,
        ILogger<GuildResourceSyncWorker> logger)
    {
        _client = client;
        _syncService = syncService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_client.ConnectionState == ConnectionState.Connected)
                {
                    await _syncService.ProcessPendingSyncRequestsAsync(_client, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing pending guild resource sync requests.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }
}
