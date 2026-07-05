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

    public GuildMaintenanceWorker(
        DiscordSocketClient client,
        CommandPanelSyncService commandPanelSyncService,
        TicketChannelCleanupService ticketCleanupService,
        TicketOutboundMessageService ticketOutboundMessageService,
        ILogger<GuildMaintenanceWorker> logger,
        WorkflowActionSyncService workflowActions)
    {
        _client = client;
        _commandPanelSyncService = commandPanelSyncService;
        _ticketCleanupService = ticketCleanupService;
        _ticketOutboundMessageService = ticketOutboundMessageService;
        _logger = logger;
        _workflowActions = workflowActions;
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
