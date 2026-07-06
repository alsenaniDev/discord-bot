using DiscordBot.Bot.Api;
using DiscordBot.Bot.Commands;
using DiscordBot.Bot.Configuration;
using DiscordBot.Bot.Extensions;
using DiscordBot.Bot.Services;
using Discord;
using Discord.WebSocket;
using DiscordBot.Bot.Music;
using Lavalink4NET.Extensions;

var builder = Host.CreateApplicationBuilder(args);

// Local overrides (gitignored). Loaded after default JSON; env vars still win in Production.
builder.Configuration.AddJsonFile(
    $"appsettings.{builder.Environment.EnvironmentName}.local.json",
    optional: true,
    reloadOnChange: true);

builder.Services.Configure<BotOptions>(builder.Configuration.GetSection(BotOptions.SectionName));
builder.Services.Configure<ApiOptions>(builder.Configuration.GetSection(ApiOptions.SectionName));
builder.Services.Configure<PlatformOptions>(builder.Configuration.GetSection(PlatformOptions.SectionName));
builder.Services.Configure<LavalinkOptions>(builder.Configuration.GetSection(LavalinkOptions.SectionName));
builder.Services.Configure<DiscordActivityOptions>(builder.Configuration.GetSection(DiscordActivityOptions.SectionName));

builder.Services.AddSingleton<DiscordSocketClient>(_ =>
{
    var config = new DiscordSocketConfig
    {
        GatewayIntents = GatewayIntents.Guilds
            | GatewayIntents.GuildMembers
            | GatewayIntents.GuildVoiceStates
            | GatewayIntents.GuildMessages
            | GatewayIntents.DirectMessages
            | GatewayIntents.MessageContent
    };

    return new DiscordSocketClient(config);
});

var lavalink = builder.Configuration.GetSection(LavalinkOptions.SectionName).Get<LavalinkOptions>() ?? new LavalinkOptions();
builder.Services.ConfigureLavalink(x =>
{
    var scheme = lavalink.Secure ? "https" : "http";
    x.BaseAddress = new Uri($"{scheme}://{lavalink.Host}:{lavalink.Port}");
    x.Passphrase = lavalink.Password;
    x.ReadyTimeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddHttpClient<BotApiClient>();
builder.Services.AddSingleton<EmbedBuilderService>();
builder.Services.AddSingleton<ComponentBuilderService>();
builder.Services.AddSingleton<SlashCommandHandlers>();
builder.Services.AddSingleton<TicketCommandHandlers>();
builder.Services.AddSingleton<TicketInteractionHandlers>();
builder.Services.AddSingleton<PanelInteractionHandlers>();
builder.Services.AddSingleton<WorkflowConversationService>();
builder.Services.AddSingleton<WorkflowActionSyncService>();
builder.Services.AddSingleton<GamesHubInteractionService>();
builder.Services.AddSingleton<GamesContextCache>();
builder.Services.AddSingleton<DiscordActivityLaunchService>();
builder.Services.AddSingleton<GameResultPublishService>();
builder.Services.AddSingleton<RoulettePublishService>();
builder.Services.AddSingleton<MusicSessionManager>();
builder.Services.AddSingleton<IAudioSearchService, LavalinkAudioSearchService>();
builder.Services.AddSingleton<IMusicService, MusicService>();
builder.Services.AddSingleton<ModerationCommandHandlers>();
builder.Services.AddSingleton<ReactionRoleCommandHandlers>();
builder.Services.AddSingleton<ReactionRoleInteractionHandlers>();
builder.Services.AddSingleton<ModuleGuard>();
builder.Services.AddSingleton<DiscordLogDeliveryService>();
builder.Services.AddSingleton<TicketArchiveService>();
builder.Services.AddSingleton<BotLogWriter>();
builder.Services.AddSingleton<WelcomeMessageService>();
builder.Services.AddSingleton<ResourceSyncService>();
builder.Services.AddHostedService<DiscordBotHostedService>();
builder.Services.AddLavalink();
builder.Services.AddHostedService<MusicPlaybackCoordinator>();
builder.Services.AddSingleton<CommandPanelSyncService>();
builder.Services.AddSingleton<TicketChannelCleanupService>();
builder.Services.AddSingleton<TicketOutboundMessageService>();
builder.Services.AddSingleton<AutoReplyMessageService>();
builder.Services.AddSingleton<TicketTimelineMessageService>();
builder.Services.AddHostedService<GuildMaintenanceWorker>();
builder.Services.AddHostedService<GuildResourceSyncWorker>();

builder.Logging.AddConsole();
builder.Logging.AddDebug();

var host = builder.Build();
host.ValidateRequiredConfiguration();
host.Run();
