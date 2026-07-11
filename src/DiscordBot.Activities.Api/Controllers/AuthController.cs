using DiscordBot.Activities.Application.Abstractions;
using DiscordBot.Activities.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiscordBot.Activities.Api.Controllers;

[AllowAnonymous, ApiController, Route("api/auth/discord")]
public class AuthController(IActivityAuthService auth) : ControllerBase
{
    [HttpPost("exchange")]
    public async Task<IActionResult> Exchange(ExchangeDiscordCodeRequest request, CancellationToken ct)
    {
        var result = await auth.ExchangeDiscordCodeAsync(request, ct);
        return StatusCode(result.StatusCode, result.Succeeded ? result.Value : new { message = result.Error });
    }
}
