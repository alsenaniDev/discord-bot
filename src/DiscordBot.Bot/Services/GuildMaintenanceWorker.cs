using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DiscordBot.Bot.Services;

/// <summary>
/// Polls the API for command panel refreshes and ticket channel cleanups.
/// </summary>
public class GuildMaintenanceWorker : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);

    private readonly DiscordSocketClient _client;
    private readonly CommandPanelSyncService _commandPanelSyncService;
    private readonly TicketChannelCleanupService _ticketCleanupService;
    private readonly TicketOutboundMessageService _ticketOutboundMessageService;
    private readonly ILogger<GuildMaintenanceWorker> _logger;
    private readonly WorkflowActionSyncService _workflowActions;
    private readonly GameResultPublishService _gameResults;
    private readonly GamesContextCache _gamesContextCache;

    public GuildMaintenanceWorker(
        DiscordSocketClient client,
        CommandPanelSyncService commandPanelSyncService,
        TicketChannelCleanupService ticketCleanupService,
        TicketOutboundMessageService ticketOutboundMessageService,
        ILogger<GuildMaintenanceWorker> logger,
        WorkflowActionSyncService workflowActions,
        GameResultPublishService gameResults,
        GamesContextCache gamesContextCache)
    {
        _client = client;
        _commandPanelSyncService = commandPanelSyncService;
        _ticketCleanupService = ticketCleanupService;
        _ticketOutboundMessageService = ticketOutboundMessageService;
        _logger = logger;
        _workflowActions = workflowActions;
        _gameResults = gameResults;
        _gamesContextCache = gamesContextCache;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_client.ConnectionState == ConnectionState.Connected)
                {
                    await _commandPanelSyncService.ProcessPendingRefreshesAsync(_client, stoppingToken);
                    await _ticketCleanupService.ProcessPendingCleanupsAsync(_client, stoppingToken);
                    await _ticketOutboundMessageService.ProcessPendingMessagesAsync(_client, stoppingToken);
                    await _workflowActions.ProcessAsync(_client, stoppingToken);
                    await _gameResults.ProcessAsync(_client, stoppingToken);
                    foreach (var guild in _client.Guilds) await _gamesContextCache.RefreshAsync(guild.Id, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing guild maintenance tasks.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }
}
