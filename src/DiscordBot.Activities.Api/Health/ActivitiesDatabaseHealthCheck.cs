using DiscordBot.Activities.Infrastructure.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DiscordBot.Activities.Api.Health;

public class ActivitiesDatabaseHealthCheck(ActivitiesDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            return await db.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy("Activities database is reachable.")
                : HealthCheckResult.Unhealthy("Activities database is not reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Activities database health check failed.", ex);
        }
    }
}
