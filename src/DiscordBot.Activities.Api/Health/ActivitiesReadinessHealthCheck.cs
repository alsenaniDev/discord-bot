using DiscordBot.Activities.Infrastructure.Data;
using DiscordBot.Activities.Infrastructure.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace DiscordBot.Activities.Api.Health;

public class ActivitiesReadinessHealthCheck(
    ActivitiesDbContext db,
    IOptions<ActivitiesJwtOptions> jwt,
    IOptions<DiscordActivityOptions> discord,
    IOptions<PlatformApiOptions> platform) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var failures = new List<string>();
        if (string.IsNullOrWhiteSpace(jwt.Value.SigningKey) || jwt.Value.SigningKey.Length < 32) failures.Add("JWT signing key is missing or too short.");
        if (string.IsNullOrWhiteSpace(jwt.Value.Issuer) || string.IsNullOrWhiteSpace(jwt.Value.Audience)) failures.Add("JWT issuer/audience is missing.");
        if (string.IsNullOrWhiteSpace(discord.Value.ClientId) || string.IsNullOrWhiteSpace(discord.Value.ClientSecret)) failures.Add("Discord Activity OAuth configuration is incomplete.");
        if (string.IsNullOrWhiteSpace(platform.Value.BaseUrl) || string.IsNullOrWhiteSpace(platform.Value.ServiceToken)) failures.Add("Platform API configuration is incomplete.");

        try
        {
            if (!await db.Database.CanConnectAsync(cancellationToken)) failures.Add("Activities database is not reachable.");
            else
            {
                var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
                if (pending.Count > 0) failures.Add($"Activities database has {pending.Count} pending migrations.");
            }
        }
        catch (Exception ex)
        {
            failures.Add($"Activities database readiness failed: {ex.Message}");
        }

        return failures.Count == 0
            ? HealthCheckResult.Healthy("Activities pilot dependencies are ready.")
            : HealthCheckResult.Unhealthy("Activities pilot dependencies are not ready.", data: failures.Select((x, i) => new KeyValuePair<string, object>($"failure_{i + 1}", x)).ToDictionary());
    }
}
