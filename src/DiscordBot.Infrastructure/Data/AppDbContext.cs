using DiscordBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DiscordBot.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Guild> Guilds => Set<Guild>();
    public DbSet<GuildSettings> GuildSettings => Set<GuildSettings>();
    public DbSet<GuildMusicSettings> GuildMusicSettings => Set<GuildMusicSettings>();
    public DbSet<LogEntry> LogEntries => Set<LogEntry>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<DiscordChannel> DiscordChannels => Set<DiscordChannel>();
    public DbSet<DiscordRole> DiscordRoles => Set<DiscordRole>();
    public DbSet<DiscordGuildMember> DiscordGuildMembers => Set<DiscordGuildMember>();
    public DbSet<Warning> Warnings => Set<Warning>();
    public DbSet<ModerationCase> ModerationCases => Set<ModerationCase>();
    public DbSet<Module> Modules => Set<Module>();
    public DbSet<GuildModule> GuildModules => Set<GuildModule>();
    public DbSet<ReactionRole> ReactionRoles => Set<ReactionRole>();
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<GuildSubscription> GuildSubscriptions => Set<GuildSubscription>();
    public DbSet<PlatformAdmin> PlatformAdmins => Set<PlatformAdmin>();
    public DbSet<PlanUpgradeRequest> PlanUpgradeRequests => Set<PlanUpgradeRequest>();
    public DbSet<GuildPermissionRole> GuildPermissionRoles => Set<GuildPermissionRole>();
    public DbSet<AutoReplyRule> AutoReplyRules => Set<AutoReplyRule>();
    public DbSet<TicketOutboundMessage> TicketOutboundMessages => Set<TicketOutboundMessage>();
    public DbSet<TicketTimelineEvent> TicketTimelineEvents => Set<TicketTimelineEvent>();
    public DbSet<GuildPanel> GuildPanels => Set<GuildPanel>();
    public DbSet<GuildPanelButton> GuildPanelButtons => Set<GuildPanelButton>();
    public DbSet<GuildWorkflow> GuildWorkflows => Set<GuildWorkflow>();
    public DbSet<WorkflowQuestion> WorkflowQuestions => Set<WorkflowQuestion>();
    public DbSet<WorkflowSubmission> WorkflowSubmissions => Set<WorkflowSubmission>();
    public DbSet<WorkflowApprovalAction> WorkflowApprovalActions => Set<WorkflowApprovalAction>();
    public DbSet<WorkflowPendingAction> WorkflowPendingActions => Set<WorkflowPendingAction>();
    public DbSet<PlatformGameDefinition> PlatformGameDefinitions => Set<PlatformGameDefinition>();
    public DbSet<GuildGamesSettings> GuildGamesSettings => Set<GuildGamesSettings>();
    public DbSet<GuildGameSetting> GuildGameSettings => Set<GuildGameSetting>();
    public DbSet<GameSession> GameSessions => Set<GameSession>();
    public DbSet<GamePlayer> GamePlayers => Set<GamePlayer>();
    public DbSet<GameContent> GameContent => Set<GameContent>();
    public DbSet<GameResultPublishAction> GameResultPublishActions => Set<GameResultPublishAction>();
    public DbSet<GameVersion> GameVersions => Set<GameVersion>();
    public DbSet<GameSandboxAccess> GameSandboxAccess => Set<GameSandboxAccess>();
    public DbSet<GameEvent> GameEvents => Set<GameEvent>();
    public DbSet<GameBotPublishAction> GameBotPublishActions => Set<GameBotPublishAction>();
    public DbSet<GameRuntimeToken> GameRuntimeTokens => Set<GameRuntimeToken>();
    public DbSet<RouletteGuildSettings> RouletteGuildSettings => Set<RouletteGuildSettings>();
    public DbSet<GameWallet> GameWallets => Set<GameWallet>();
    public DbSet<GameWalletTransaction> GameWalletTransactions => Set<GameWalletTransaction>();
    public DbSet<WalletReservation> WalletReservations => Set<WalletReservation>();
    public DbSet<RouletteRoom> RouletteRooms => Set<RouletteRoom>();
    public DbSet<RouletteRoomPlayer> RouletteRoomPlayers => Set<RouletteRoomPlayer>();
    public DbSet<RouletteRoundAction> RouletteRoundActions => Set<RouletteRoundAction>();
    public DbSet<RouletteJoinIntent> RouletteJoinIntents => Set<RouletteJoinIntent>();
    public DbSet<RoulettePublishAction> RoulettePublishActions => Set<RoulettePublishAction>();
    public DbSet<GamePowerUpDefinition> GamePowerUpDefinitions => Set<GamePowerUpDefinition>();
    public DbSet<GuildPowerUpSetting> GuildPowerUpSettings => Set<GuildPowerUpSetting>();
    public DbSet<PlayerPowerUpInventory> PlayerPowerUpInventories => Set<PlayerPowerUpInventory>();
    public DbSet<RoulettePowerUpUsage> RoulettePowerUpUsages => Set<RoulettePowerUpUsage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
