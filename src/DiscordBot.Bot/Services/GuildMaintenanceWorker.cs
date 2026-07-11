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
    private readonly RoulettePublishService _roulettePublish;
    private readonly ActivitiesRouletteAnnouncementService _activitiesRouletteAnnouncements;
    private readonly GamesContextCache _gamesContextCache;
    private readonly DiscordActivityLaunchService _activityLauncher;
    private DateTimeOffset _lastActivityDiagnosticsLoggedUtc = DateTimeOffset.MinValue;

    public GuildMaintenanceWorker(
        DiscordSocketClient client,
        CommandPanelSyncService commandPanelSyncService,
        TicketChannelCleanupService ticketCleanupService,
        TicketOutboundMessageService ticketOutboundMessageService,
        ILogger<GuildMaintenanceWorker> logger,
        WorkflowActionSyncService workflowActions,
        GameResultPublishService gameResults,
        RoulettePublishService roulettePublish,
        ActivitiesRouletteAnnouncementService activitiesRouletteAnnouncements,
        GamesContextCache gamesContextCache,
        DiscordActivityLaunchService activityLauncher)
    {
        _client = client;
        _commandPanelSyncService = commandPanelSyncService;
        _ticketCleanupService = ticketCleanupService;
        _ticketOutboundMessageService = ticketOutboundMessageService;
        _logger = logger;
        _workflowActions = workflowActions;
        _gameResults = gameResults;
        _roulettePublish = roulettePublish;
        _activitiesRouletteAnnouncements = activitiesRouletteAnnouncements;
        _gamesContextCache = gamesContextCache;
        _activityLauncher = activityLauncher;
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
                    await _roulettePublish.ProcessAsync(_client, stoppingToken);
                    await _activitiesRouletteAnnouncements.ProcessAsync(_client, stoppingToken);
                    foreach (var guild in _client.Guilds) await _gamesContextCache.RefreshAsync(guild.Id, stoppingToken);
                    await _activityLauncher.RefreshAvailabilityAsync(ct: stoppingToken);
                    LogActivityDiagnosticsIfDue();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing guild maintenance tasks.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private void LogActivityDiagnosticsIfDue()
    {
        var now = DateTimeOffset.UtcNow;
        if (now - _lastActivityDiagnosticsLoggedUtc < TimeSpan.FromMinutes(5)) return;
        _lastActivityDiagnosticsLoggedUtc = now;
        var diagnostics = _activityLauncher.GetDiagnostics();
        _logger.LogInformation(
            "Discord Activity launch diagnostics: successful={SuccessfulLaunches}, failed={FailedLaunches}, rateLimited={RateLimitedLaunches}, averageLatencyMs={AverageLatencyMs:F1}, lastSuccess={LastSuccessfulLaunchUtc}, lastRateLimit={LastRateLimitUtc}, retryAfterMs={RetryAfterMs}, launchInFlight={LaunchInFlight}, userCooldowns={UserCooldownCount}, guildCooldowns={GuildCooldownCount}, availabilityLoaded={AvailabilityLoaded}, embedded={IsEmbedded}, applicationId={ApplicationId}, availabilityAgeSeconds={AvailabilityAgeSeconds}, availabilityTtlSeconds={AvailabilityTtlSeconds}.",
            diagnostics.SuccessfulLaunches,
            diagnostics.FailedLaunches,
            diagnostics.RateLimitedLaunches,
            diagnostics.AverageLatencyMs,
            diagnostics.LastSuccessfulLaunchUtc,
            diagnostics.LastRateLimitUtc,
            diagnostics.LastRetryAfter?.TotalMilliseconds,
            diagnostics.LaunchInFlight,
            diagnostics.UserCooldownCount,
            diagnostics.GuildCooldownCount,
            diagnostics.AvailabilityLoaded,
            diagnostics.IsEmbedded,
            diagnostics.ApplicationId,
            diagnostics.AvailabilityLoaded ? (int)(now - diagnostics.AvailabilityLoadedAtUtc).TotalSeconds : null,
            diagnostics.AvailabilityCacheSeconds);
    }
}
