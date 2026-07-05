using DiscordBot.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiscordBot.Api.Controllers;

[AllowAnonymous, ApiController, Route("api/discord/activity")]
public class DiscordActivityController(IDiscordActivityAuthService auth, ILogger<DiscordActivityController> logger) : ControllerBase
{
    [HttpPost("token")]
    public async Task<IActionResult> Token(ActivityCodeRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Code)) return BadRequest(new { message = "رمز التفويض مطلوب." });
        try
        {
            var token = await auth.ExchangeCodeAsync(request.Code, ct);
            return token is null ? BadRequest(new { message = "تعذر تسجيل الدخول إلى ديسكورد. أغلق الواجهة وحاول مرة ثانية." }) : Ok(token);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Discord Activity OAuth code exchange failed.");
            return StatusCode(502, new { message = "تعذر التواصل مع ديسكورد الآن. حاول مرة ثانية بعد قليل." });
        }
    }
}

public sealed class ActivityCodeRequest { public string Code { get; set; } = string.Empty; }
