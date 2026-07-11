using DiscordBot.Activities.Application.Abstractions;
using DiscordBot.Activities.Infrastructure.Auth;
using DiscordBot.Activities.Infrastructure.Data;
using DiscordBot.Activities.Infrastructure.Options;
using DiscordBot.Activities.Infrastructure.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DiscordBot.Activities.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddActivitiesInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DiscordActivityOptions>(configuration.GetSection(DiscordActivityOptions.SectionName));
        services.Configure<ActivitiesJwtOptions>(configuration.GetSection(ActivitiesJwtOptions.SectionName));
        services.Configure<PlatformApiOptions>(configuration.GetSection(PlatformApiOptions.SectionName));

        var connectionString = configuration.GetConnectionString("ActivitiesDatabase")
            ?? throw new InvalidOperationException("Connection string 'ActivitiesDatabase' not found.");
        services.AddDbContext<ActivitiesDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<IActivityTokenService, ActivityTokenService>();
        services.AddScoped<IActivitySessionService, ActivitySessionService>();
        services.AddScoped<IRouletteRuntimeService, RouletteRuntimeService>();
        services.AddHostedService<RouletteWalletReconciliationService>();
        services.AddHostedService<RoulettePayoutReconciliationService>();
        services.AddHttpClient<IActivityAuthService, DiscordActivityAuthService>();
        services.AddHttpClient<IPlatformApiClient, PlatformApiClient>((provider, client) =>
        {
            var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<PlatformApiOptions>>().Value;
            if (!string.IsNullOrWhiteSpace(options.BaseUrl)) client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
        });

        return services;
    }
}
