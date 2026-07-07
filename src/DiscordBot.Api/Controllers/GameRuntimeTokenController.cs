using DiscordBot.Infrastructure.Auth;
using DiscordBot.Infrastructure.Models;
using DiscordBot.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiscordBot.Api.Controllers;

[AllowAnonymous, ApiController, Route("api/games/runtime")]
public class GameRuntimeTokenController(IDiscordActivityAuthService auth, IGamePluginService plugins) : ControllerBase
{
    [HttpPost("token")]
    public async Task<IActionResult> Issue(IssueGameRuntimeTokenRequest request, CancellationToken ct)
    {
        var user = await UserAsync(ct);
        if (user is null) return Unauthorized(new { message = "انتهت صلاحية تسجيل الدخول. افتح مركز الألعاب مرة ثانية." });
        return Result(await plugins.IssueRuntimeTokenAsync(request, user, ct));
    }

    private async Task<ActivityDiscordUser?> UserAsync(CancellationToken ct)
    {
        var header = Request.Headers.Authorization.ToString();
        return header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? await auth.ValidateAccessTokenAsync(header[7..].Trim(), ct) : null;
    }

    private ObjectResult Result<T>(GameHubResult<T> result) => StatusCode(result.StatusCode, result.Succeeded ? result.Value : new { message = result.Error });
}
