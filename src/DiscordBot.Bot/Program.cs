using DiscordBot.Bot.Api;
using DiscordBot.Bot.Commands;
using DiscordBot.Bot.Configuration;
using DiscordBot.Bot.Extensions;
using DiscordBot.Bot.Services;
using Discord;
using Discord.WebSocket;

var builder = Host.CreateApplicationBuilder(args);

// Local overrides (gitignored). Loaded after default JSON; env vars still win in Production.
builder.Configuration.AddJsonFile(
    $"appsettings.{builder.Environment.EnvironmentName}.local.json",
    optional: true,
    reloadOnChange: true);

builder.Services.Configure<BotOptions>(builder.Configuration.GetSection(BotOptions.SectionName));
builder.Services.Configure<ApiOptions>(builder.Configuration.GetSection(ApiOptions.SectionName));
builder.Services.Configure<PlatformOptions>(builder.Configuration.GetSection(PlatformOptions.SectionName));

builder.Services.AddSingleton<DiscordSocketClient>(_ =>
{
    var config = new DiscordSocketConfig
    {
        GatewayIntents = GatewayIntents.Guilds
            | GatewayIntents.GuildMembers
            | GatewayIntents.GuildMessages
            | GatewayIntents.DirectMessages
            | GatewayIntents.MessageContent
    };

    return new DiscordSocketClient(config);
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
