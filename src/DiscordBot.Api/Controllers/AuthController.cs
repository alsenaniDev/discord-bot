using System.Security.Claims;
using DiscordBot.Api.Extensions;
using DiscordBot.Api.Models;
using DiscordBot.Domain.Entities;
using DiscordBot.Infrastructure.Auth;
using DiscordBot.Infrastructure.Options;
using DiscordBot.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DiscordBot.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IDiscordOAuthService _discordOAuthService;
    private readonly IAuthCodeService _authCodeService;
    private readonly IPlatformAdminService _platformAdminService;
    private readonly DiscordOptions _discordOptions;

    public AuthController(
        IAuthService authService,
        IDiscordOAuthService discordOAuthService,
        IAuthCodeService authCodeService,
        IPlatformAdminService platformAdminService,
        IOptions<DiscordOptions> discordOptions)
    {
        _authService = authService;
        _discordOAuthService = discordOAuthService;
        _authCodeService = authCodeService;
        _platformAdminService = platformAdminService;
        _discordOptions = discordOptions.Value;
    }

    /// <summary>
    /// Step 1 of login: dashboard calls this to get the Discord authorize URL.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("discord/login")]
    public ActionResult<DiscordLoginResponse> GetDiscordLoginUrl()
    {
        var (authorizeUrl, _) = _discordOAuthService.CreateLoginRequest();
        return Ok(new DiscordLoginResponse { Url = authorizeUrl });
    }

    /// <summary>
    /// Step 2 of login: Discord redirects here with ?code=&amp;state= after the user approves.
    /// Redirects to the dashboard with a one-time code (not the JWT).
    /// </summary>
    [AllowAnonymous]
    [HttpGet("discord/callback")]
    public async Task<IActionResult> DiscordCallback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
        {
            return BadRequest(new { message = "Missing OAuth code or state." });
        }

        try
        {
            var result = await _authService.SignInWithDiscordAsync(code, state, cancellationToken);
            var exchangeCode = _authCodeService.CreateExchangeCode(result.AccessToken);

            var dashboardBase = _discordOptions.DashboardUrl.TrimEnd('/');
            var redirectUrl =
                $"{dashboardBase}/auth/callback" +
                $"?code={Uri.EscapeDataString(exchangeCode)}";

            return Redirect(redirectUrl);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Step 3 of login: dashboard exchanges the one-time code for a JWT (in JSON body, not URL).
    /// </summary>
    [AllowAnonymous]
    [HttpPost("token")]
    public ActionResult<ExchangeTokenResponse> ExchangeToken([FromBody] ExchangeTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return BadRequest(new { message = "Missing exchange code." });
        }

        var accessToken = _authCodeService.ConsumeExchangeCode(request.Code);
        if (accessToken is null)
        {
            return BadRequest(new { message = "Invalid or expired exchange code." });
        }

        return Ok(new ExchangeTokenResponse { AccessToken = accessToken });
    }

    /// <summary>
    /// Returns the logged-in user. Requires Authorization: Bearer {jwt}.
    /// </summary>
    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserProfileDto>> GetCurrentUser(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var user = await _authService.GetUserByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = "User not found." });
        }

        var isAdmin = await _platformAdminService.IsAdminAsync(user.DiscordUserId, cancellationToken);

        return Ok(MapToProfile(user, isAdmin));
    }

    private static UserProfileDto MapToProfile(User user, bool isAdmin) =>
        new()
        {
            Id = user.Id,
            DiscordUserId = user.DiscordUserId,
            Username = user.Username,
            GlobalName = user.GlobalName,
            AvatarUrl = user.AvatarUrl,
            LastLoginAt = user.LastLoginAt,
            IsAdmin = isAdmin
        };
}
