using Discord;
using Discord.WebSocket;
using DiscordBot.Bot.Api;
using DiscordBot.Bot.Api.Models;
using DiscordBot.Bot.Commands;
using DiscordBot.Bot.Configuration;
using DiscordBot.Domain.Constants;
using DiscordBot.Domain.Enums;
using DiscordBot.Bot.Services;
using DiscordBot.Bot.UI;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DiscordBot.Bot.Music;

namespace DiscordBot.Bot.Services;

/// <summary>
/// Connects to Discord Gateway and wires events (ready, interactions, joins).
/// </summary>
public class DiscordBotHostedService : IHostedService
{
    private readonly DiscordSocketClient _client;
    private readonly BotApiClient _apiClient;
    private readonly SlashCommandHandlers _commandHandlers;
    private readonly TicketCommandHandlers _ticketHandlers;
    private readonly TicketInteractionHandlers _ticketInteractionHandlers;
    private readonly PanelInteractionHandlers _panelInteractionHandlers;
    private readonly ModerationCommandHandlers _moderationHandlers;
    private readonly ReactionRoleCommandHandlers _reactionRoleHandlers;
    private readonly ReactionRoleInteractionHandlers _reactionRoleInteractionHandlers;
    private readonly EmbedBuilderService _embeds;
    private readonly WelcomeMessageService _welcomeService;
    private readonly ResourceSyncService _resourceSyncService;
    private readonly ModuleGuard _moduleGuard;
    private readonly BotLogWriter _logWriter;
    private readonly AutoReplyMessageService _autoReplyMessageService;
    private readonly TicketTimelineMessageService _ticketTimelineMessageService;
    private readonly WorkflowConversationService _workflowConversations;
    private readonly IMusicService _music;
    private readonly GamesHubInteractionService _games;
    private readonly GamesContextCache _gamesContextCache;
    private readonly DiscordActivityLaunchService _activityLauncher;
    private readonly BotOptions _botOptions;
    private readonly ILogger<DiscordBotHostedService> _logger;
    private int _readyStartupWorkStarted;

    public DiscordBotHostedService(
        DiscordSocketClient client,
        BotApiClient apiClient,
        SlashCommandHandlers commandHandlers,
        TicketCommandHandlers ticketHandlers,
        TicketInteractionHandlers ticketInteractionHandlers,
        PanelInteractionHandlers panelInteractionHandlers,
        ModerationCommandHandlers moderationHandlers,
        ReactionRoleCommandHandlers reactionRoleHandlers,
        ReactionRoleInteractionHandlers reactionRoleInteractionHandlers,
        EmbedBuilderService embeds,
        WelcomeMessageService welcomeService,
        ResourceSyncService resourceSyncService,
        ModuleGuard moduleGuard,
        BotLogWriter logWriter,
        AutoReplyMessageService autoReplyMessageService,
        TicketTimelineMessageService ticketTimelineMessageService,
        WorkflowConversationService workflowConversations,
        IMusicService music,
        GamesHubInteractionService games,
        GamesContextCache gamesContextCache,
        DiscordActivityLaunchService activityLauncher,
        IOptions<BotOptions> botOptions,
        ILogger<DiscordBotHostedService> logger)
    {
        _client = client;
        _apiClient = apiClient;
        _commandHandlers = commandHandlers;
        _ticketHandlers = ticketHandlers;
        _ticketInteractionHandlers = ticketInteractionHandlers;
        _panelInteractionHandlers = panelInteractionHandlers;
        _moderationHandlers = moderationHandlers;
        _reactionRoleHandlers = reactionRoleHandlers;
        _reactionRoleInteractionHandlers = reactionRoleInteractionHandlers;
        _embeds = embeds;
        _welcomeService = welcomeService;
        _resourceSyncService = resourceSyncService;
        _moduleGuard = moduleGuard;
        _logWriter = logWriter;
        _autoReplyMessageService = autoReplyMessageService;
        _ticketTimelineMessageService = ticketTimelineMessageService;
        _workflowConversations = workflowConversations;
        _music = music;
        _games = games;
        _gamesContextCache = gamesContextCache;
        _activityLauncher = activityLauncher;
        _botOptions = botOptions.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_botOptions.Token))
        {
            throw new InvalidOperationException("Discord:Token is not configured.");
        }

        _logger.LogInformation(
            "Starting Discord bot worker. Connecting to Discord Gateway (API base: configured).");

        _client.Log += message =>
        {
            var level = message.Severity switch
            {
                LogSeverity.Critical or LogSeverity.Error => LogLevel.Error,
                LogSeverity.Warning => LogLevel.Warning,
                LogSeverity.Info => LogLevel.Information,
                _ => LogLevel.Debug
            };

            _logger.Log(level, "[{Source}] {Message}", message.Source, message.Message);
            return Task.CompletedTask;
        };

        _client.Ready += OnReadyAsync;
        _client.InteractionCreated += OnInteractionCreatedAsync;
        _client.JoinedGuild += OnJoinedGuildAsync;
        _client.UserJoined += OnUserJoinedAsync;
        _client.MessageReceived += OnMessageReceivedAsync;

        await _client.LoginAsync(TokenType.Bot, _botOptions.Token);
        await _client.StartAsync();

        _logger.LogInformation("Discord Gateway client started.");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _client.StopAsync();
        await _client.LogoutAsync();
    }

    private async Task OnReadyAsync()
    {
        _logger.LogInformation("Logged in as {Username} ({Id})", _client.CurrentUser.Username, _client.CurrentUser.Id);

        if (Interlocked.Exchange(ref _readyStartupWorkStarted, 1) == 1)
        {
            _logger.LogInformation(
                "Discord Ready received again for bot {BotId}; skipping one-time startup REST work (global command overwrite, guild command overwrite, guild sync, activity availability refresh).",
                _client.CurrentUser.Id);
            return;
        }

        _logger.LogInformation(
            "Running one-time Ready startup work for bot {BotId}. If production shows multiple copies of this log at the same time, multiple bot instances are running.",
            _client.CurrentUser.Id);

        await SlashCommandRegistration.RegisterGlobalCommandsAsync(_client);
        _logger.LogInformation("Global slash commands registered.");
        await _activityLauncher.RefreshAvailabilityAsync();

        var guilds = _client.Guilds;
        _logger.LogInformation("Syncing {GuildCount} guild(s) with the API on startup.", guilds.Count);

        foreach (var guild in guilds)
        {
            try
            {
                await SyncGuildOnStartupAsync(guild);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to sync guild {GuildId} ({GuildName}) during startup.",
                    guild.Id,
                    guild.Name);
            }
        }
    }

    private async Task SyncGuildOnStartupAsync(SocketGuild guild)
    {
        _logger.LogDebug("Registering guild {GuildId} ({GuildName}) with the API.", guild.Id, guild.Name);

        await RegisterGuildWithApiAsync(guild);
        await SlashCommandRegistration.RegisterGuildCommandsAsync(guild);
        await _gamesContextCache.RefreshAsync(guild.Id);

        _logger.LogInformation(
            "Guild {GuildId} ({GuildName}) synced on startup.",
            guild.Id,
            guild.Name);
    }

    private Task OnInteractionCreatedAsync(SocketInteraction interaction)
    {
        // Discord.Net invokes this callback on its gateway dispatch loop. Lavalink joins
        // require that same loop to process the resulting voice state/server events.
        // Dispatch the complete interaction pipeline so a command can never block them.
        _ = Task.Run(() => ProcessInteractionAsync(interaction));
        return Task.CompletedTask;
    }

    private async Task ProcessInteractionAsync(SocketInteraction interaction)
    {
        try
        {
            switch (interaction)
            {
                case SocketSlashCommand command:
                    await HandleSlashCommandAsync(interaction, command);
                    break;
                case SocketMessageComponent component:
                    await HandleComponentAsync(component);
                    break;
                case SocketModal modal:
                    await HandleModalAsync(modal);
                    break;
            }
        }
        catch (Exception ex)
        {
            if (ex is Discord.Net.HttpException httpException
                && httpException.DiscordCode.HasValue
                && (int)httpException.DiscordCode.Value is 10062 or 40060)
            {
                _logger.LogWarning("Interaction {InteractionId} was expired or already acknowledged; skipping a second response.", interaction.Id);
                return;
            }
            _logger.LogError(ex, "Error handling interaction {Type}", interaction.Type);

            try { await InteractionResponseHelper.RespondUnexpectedErrorAsync(interaction, _embeds); }
            catch (Exception responseError) { _logger.LogWarning(responseError, "Could not send the unexpected-error interaction response."); }
        }
    }

    private async Task HandleSlashCommandAsync(SocketInteraction interaction, SocketSlashCommand command)
    {
        switch (command.CommandName)
        {
            case "ping":
                await _commandHandlers.HandlePingAsync(interaction);
                break;
            case "server":
                await _commandHandlers.HandleServerAsync(interaction);
                break;
            case "setup":
                await _commandHandlers.HandleSetupAsync(interaction);
                break;
            case "sync":
                await _commandHandlers.HandleSyncAsync(interaction);
                break;
            case "ticket":
                await HandleTicketCommandAsync(interaction, command);
                break;
            case "warn":
                await _moderationHandlers.HandleWarnAsync(interaction, command);
                break;
            case "warnings":
                await _moderationHandlers.HandleWarningsAsync(interaction, command);
                break;
            case "clear":
                await _moderationHandlers.HandleClearAsync(interaction, command);
                break;
            case "kick":
                await _moderationHandlers.HandleKickAsync(interaction, command);
                break;
            case "reaction-role":
                await HandleReactionRoleCommandAsync(interaction, command);
                break;
            case "music":
                await HandleMusicCommandAsync(interaction, command);
                break;
            case "games":
                await _games.ShowHubAsync(interaction);
                break;
            default:
                await InteractionResponseHelper.RespondErrorAsync(
                    interaction,
                    _embeds,
                    "Unknown command",
                    "That command is not recognized.");
                break;
        }
    }

    private async Task HandleComponentAsync(SocketMessageComponent component)
    {
        var customId = component.Data.CustomId;

        if (customId.StartsWith(GamesHubInteractionService.ComponentPrefix, StringComparison.Ordinal))
        {
            await _games.HandleButtonAsync(component);
            return;
        }

        if (customId.StartsWith(DiscordCustomIds.MusicPrefix, StringComparison.Ordinal))
        {
            await _music.HandleButtonAsync(component, customId[DiscordCustomIds.MusicPrefix.Length..]);
            return;
        }

        if (DiscordCustomIds.TryParseWorkflowQuestionAnswer(customId, out var answerWorkflowId, out var answerConversationToken, out var answerQuestionToken, out var answer))
        {
            await _workflowConversations.AnswerFromButtonAsync(component, answerWorkflowId, answerConversationToken, answerQuestionToken, answer);
            return;
        }

        if (DiscordCustomIds.TryParseWorkflowConversationCancel(customId, out var cancelWorkflowId, out var conversationId))
        {
            await _workflowConversations.CancelFromButtonAsync(component, cancelWorkflowId, conversationId);
            return;
        }

        if (customId == DiscordCustomIds.TicketCreate
            || customId.StartsWith(DiscordCustomIds.TicketClosePrefix, StringComparison.Ordinal))
        {
            await _ticketInteractionHandlers.HandleButtonAsync(component);
            return;
        }

        if (customId.StartsWith(DiscordCustomIds.PanelPrefix, StringComparison.Ordinal))
        {
            await _panelInteractionHandlers.HandleButtonAsync(component);
            return;
        }

        if (DiscordCustomIds.TryParseWorkflowControl(customId, out var confirm, out var workflowId, out var guildId))
        {
            await _panelInteractionHandlers.HandleWorkflowControlAsync(component, confirm, workflowId, guildId);
            return;
        }

        if (customId.StartsWith(DiscordCustomIds.TicketSelectPrefix, StringComparison.Ordinal))
        {
            await _ticketInteractionHandlers.HandleSelectMenuAsync(component);
            return;
        }

        if (customId.StartsWith(DiscordCustomIds.ReactionRoleTogglePrefix, StringComparison.Ordinal))
        {
            await _reactionRoleInteractionHandlers.HandleButtonAsync(component);
        }
    }

    private async Task HandleReactionRoleCommandAsync(SocketInteraction interaction, SocketSlashCommand command)
    {
        var subcommand = command.Data.Options.FirstOrDefault()?.Name;
        switch (subcommand)
        {
            case "create":
                await _reactionRoleHandlers.HandleCreateAsync(interaction, command);
                break;
            default:
                await InteractionResponseHelper.RespondErrorAsync(
                    interaction,
                    _embeds,
                    "Unknown subcommand",
                    "Use `/reaction-role create`.");
                break;
        }
    }

    private async Task HandleMusicCommandAsync(SocketInteraction interaction, SocketSlashCommand command)
    {
        var subcommand = command.Data.Options.FirstOrDefault();
        switch (subcommand?.Name)
        {
            case "play": await _music.PlayAsync(interaction, subcommand.Options.First(x => x.Name == "query").Value?.ToString() ?? string.Empty); break;
            case "skip": await _music.SkipAsync(interaction); break;
            case "stop": await _music.StopAsync(interaction); break;
            case "pause": await _music.PauseAsync(interaction); break;
            case "resume": await _music.ResumeAsync(interaction); break;
            case "queue": await _music.ShowQueueAsync(interaction); break;
            case "nowplaying": await _music.ShowNowPlayingAsync(interaction); break;
            default: await InteractionResponseHelper.RespondErrorAsync(interaction, _embeds, "Unknown subcommand", "Use a `/music` subcommand."); break;
        }
    }

    private async Task HandleModalAsync(SocketModal modal)
    {
        if (modal.Data.CustomId.StartsWith(DiscordCustomIds.TicketCloseModalPrefix, StringComparison.Ordinal))
        {
            await _ticketInteractionHandlers.HandleCloseModalAsync(modal);
        }
    }

    private async Task HandleTicketCommandAsync(SocketInteraction interaction, SocketSlashCommand command)
    {
        var subcommand = command.Data.Options.FirstOrDefault()?.Name;

        switch (subcommand)
        {
            case "setup":
                await _ticketHandlers.HandleSetupAsync(interaction);
                break;
            case "open":
                await _ticketHandlers.HandleOpenAsync(interaction);
                break;
            case "close":
                await _ticketHandlers.HandleCloseAsync(interaction);
                break;
            default:
                await InteractionResponseHelper.RespondErrorAsync(
                    interaction,
                    _embeds,
                    "Unknown subcommand",
                    "Use `/ticket setup`, `/ticket open`, or `/ticket close`.");
                break;
        }
    }

    private async Task OnJoinedGuildAsync(SocketGuild guild)
    {
        _logger.LogInformation("Joined guild {GuildName} ({GuildId})", guild.Name, guild.Id);

        await RegisterGuildWithApiAsync(guild);
        await SlashCommandRegistration.RegisterGuildCommandsAsync(guild);
        await _gamesContextCache.RefreshAsync(guild.Id);
    }

    private async Task OnUserJoinedAsync(SocketGuildUser user)
    {
        var guildId = user.Guild.Id.ToString();
        var settings = await _apiClient.GetSettingsAsync(guildId);
        if (settings is null)
        {
            _logger.LogDebug(
                "No settings found for guild {GuildId}. Skipping join handlers.",
                user.Guild.Id);
            return;
        }

        await _logWriter.WriteAsync(
            guildId,
            LogEventType.MemberJoined,
            $"{user.Username} joined the server.",
            targetDiscordUserId: user.Id.ToString(),
            targetDisplayName: user.Username);

        if (await _moduleGuard.IsEnabledAsync(guildId, ModuleKeys.Welcome))
        {
            await _welcomeService.SendWelcomeAsync(_client, user, settings, _logger);
        }

        if (await _moduleGuard.IsEnabledAsync(guildId, ModuleKeys.AutoRole))
        {
            await TryApplyAutoRoleAsync(user, settings);
        }
    }

    private async Task TryApplyAutoRoleAsync(SocketGuildUser user, GuildSettingsResponse settings)
    {
        if (!settings.AutoRoleEnabled || string.IsNullOrWhiteSpace(settings.AutoRoleId))
        {
            return;
        }

        if (!ulong.TryParse(settings.AutoRoleId, out var roleId))
        {
            _logger.LogWarning(
                "Auto role enabled for guild {GuildId} but AutoRoleId is invalid.",
                user.Guild.Id);
            return;
        }

        var role = user.Guild.GetRole(roleId);
        if (role is null)
        {
            _logger.LogWarning(
                "Auto role {RoleId} not found in guild {GuildId}.",
                roleId,
                user.Guild.Id);
            return;
        }

        try
        {
            await user.AddRoleAsync(role);

            await _logWriter.WriteAsync(
                user.Guild.Id.ToString(),
                LogEventType.AutoRoleAssigned,
                $"Assigned role {role.Name} to {user.Username}.",
                targetDiscordUserId: user.Id.ToString(),
                targetDisplayName: user.Username,
                metadataJson: $"{{\"roleId\":\"{role.Id}\"}}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Could not assign auto role {RoleId} to user {UserId} in guild {GuildId}.",
                roleId,
                user.Id,
                user.Guild.Id);
        }
    }

    private async Task RegisterGuildWithApiAsync(SocketGuild guild)
    {
        var iconUrl = guild.IconId is not null
            ? $"https://cdn.discordapp.com/icons/{guild.Id}/{guild.IconId}.png"
            : null;

        var request = new RegisterGuildRequest
        {
            DiscordGuildId = guild.Id.ToString(),
            Name = guild.Name,
            OwnerDiscordUserId = guild.OwnerId.ToString(),
            IconUrl = iconUrl
        };

        var result = await _apiClient.RegisterGuildAsync(request);
        if (result is null)
        {
            _logger.LogWarning("Failed to register guild {GuildId} with API.", guild.Id);
            return;
        }

        _logger.LogInformation(
            "Guild {GuildName} registered with API (IsNew={IsNew}).",
            guild.Name,
            result.IsNew);

        await _resourceSyncService.SyncGuildAsync(guild);
    }

    private async Task OnMessageReceivedAsync(SocketMessage message)
    {
        if (await _workflowConversations.HandleDmMessageAsync(message)) return;
        try
        {
            await _autoReplyMessageService.HandleMessageAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling message for auto-reply.");
        }

        try
        {
            await _ticketTimelineMessageService.HandleMessageAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording ticket timeline message.");
        }
    }
}
