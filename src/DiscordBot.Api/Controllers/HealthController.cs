using DiscordBot.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DiscordBot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class HealthController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromServices] AppDbContext db,
        [FromServices] IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!await db.Database.CanConnectAsync(cancellationToken))
            {
                return StatusCode(503, new
                {
                    status = "unhealthy",
                    service = "DiscordBot.Api",
                    database = "disconnected",
                    environment = environment.EnvironmentName,
                    timestamp = DateTime.UtcNow
                });
            }

            return Ok(new
            {
                status = "healthy",
                service = "DiscordBot.Api",
                database = "connected",
                environment = environment.EnvironmentName,
                timestamp = DateTime.UtcNow
            });
        }
        catch
        {
            return StatusCode(503, new
            {
                status = "unhealthy",
                service = "DiscordBot.Api",
                database = "error",
                environment = environment.EnvironmentName,
                timestamp = DateTime.UtcNow
            });
        }
    }
}
