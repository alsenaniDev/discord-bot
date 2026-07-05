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
