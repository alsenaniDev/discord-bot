using DiscordBot.Activities.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DiscordBot.Activities.Api.Controllers;

[ApiController, Route("api/internal/diagnostics")]
public class DiagnosticsController(ActivitiesDbContext db, IConfiguration configuration) : ControllerBase
{
    [HttpGet("pilot")]
    public async Task<IActionResult> Pilot(CancellationToken ct)
    {
        if (!Authorized()) return Unauthorized(new { message = "Invalid diagnostics service key." });
        var migrations = await db.Database.GetPendingMigrationsAsync(ct);
        var activeSessions = await db.RouletteGameSessions.CountAsync(x => x.Status == "Waiting" || x.Status == "InProgress", ct);
        var pendingWalletCommits = await db.RouletteBets.CountAsync(x => x.Status == "PendingCommit", ct);
        var pendingPayouts = await db.RoulettePayouts.CountAsync(x => x.Status == "PendingPayout" || x.Status == "RetryableFailed" || x.Status == "Processing", ct);
        var failedPayouts = await db.RoulettePayouts.CountAsync(x => x.Status == "Failed", ct);
        return Ok(new
        {
            runtimeVersion = "activities-v1",
            database = new { canConnect = await db.Database.CanConnectAsync(ct), pendingMigrations = migrations.ToArray() },
            queues = new { pendingWalletCommits, pendingPayouts, failedPayouts },
            roulette = new { activeSessions },
            pilotGuildConfiguration = new { source = "VITE_ACTIVITIES_ROULETTE_PILOT_GUILD_IDS", hardcoded = false }
        });
    }

    private bool Authorized()
    {
        var expected = configuration["ActivitiesDiagnostics:ServiceToken"] ?? configuration["PlatformApi:ServiceToken"];
        return !string.IsNullOrWhiteSpace(expected)
            && Request.Headers.TryGetValue("X-Activities-Service-Key", out var provided)
            && string.Equals(provided.ToString(), expected, StringComparison.Ordinal);
    }
}
