using DiscordBot.Infrastructure.Auth;
using DiscordBot.Infrastructure.Data;
using DiscordBot.Infrastructure.Options;
using DiscordBot.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DiscordBot.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.Configure<DiscordOptions>(configuration.GetSection(DiscordOptions.SectionName));
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<SeedOptions>(configuration.GetSection(SeedOptions.SectionName));
        services.Configure<BotOptions>(configuration.GetSection(BotOptions.SectionName));
        services.Configure<AdminOptions>(configuration.GetSection(AdminOptions.SectionName));

        services.AddMemoryCache();
        services.AddHttpClient<IDiscordOAuthService, DiscordOAuthService>();

        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthCodeService, AuthCodeService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IGuildService, GuildService>();
        services.AddScoped<IGuildResourceService, GuildResourceService>();
        services.AddScoped<ICommandPanelService, CommandPanelService>();
        services.AddScoped<ITicketService, TicketService>();
        services.AddScoped<ITicketTimelineService, TicketTimelineService>();
        services.AddScoped<ITicketReadService, TicketReadService>();
        services.AddScoped<IAutoReplyService, AutoReplyService>();
        services.AddScoped<IModerationService, ModerationService>();
        services.AddScoped<IModuleService, ModuleService>();
        services.AddScoped<ILogService, LogService>();
        services.AddScoped<IReactionRoleService, ReactionRoleService>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        services.AddScoped<IPlatformAdminService, PlatformAdminService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<IGuildAccessService, GuildAccessService>();
        services.AddScoped<IGuildPermissionResolver, GuildPermissionResolver>();
        services.AddScoped<IGuildPermissionRoleService, GuildPermissionRoleService>();
        services.AddScoped<IGuildProfileService, GuildProfileService>();
        services.AddScoped<IPlanUpgradeRequestService, PlanUpgradeRequestService>();
        services.AddScoped<IOnboardingService, OnboardingService>();
        services.AddHostedService<ModuleSeeder>();
        services.AddHostedService<SubscriptionPlanSeeder>();
        services.AddHostedService<PlatformAdminSeeder>();
        services.AddHostedService<DevelopmentDataSeeder>();

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        return services;
    }
}
