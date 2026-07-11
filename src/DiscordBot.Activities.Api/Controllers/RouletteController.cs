using DiscordBot.Activities.Application.Abstractions;
using DiscordBot.Activities.Application.Models;
using DiscordBot.Activities.Api.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DiscordBot.Activities.Api.Controllers;

[Authorize, ApiController, Route("api/roulette")]
public class RouletteController(
    IRouletteRuntimeService roulette,
    IOptions<ActivityRuntimeAuthOptions> authOptions,
    IWebHostEnvironment environment,
    ILogger<RouletteController> logger) : ControllerBase
{
    [HttpGet("capabilities")]
    public IActionResult Capabilities()
    {
        return Ok(new
        {
            runtimeVersion = "activities-v1",
            supportsWalletBets = true,
            supportsPowerUps = false,
            supportsReconnect = true
        });
    }

    [HttpPost("sessions")]
    public async Task<IActionResult> Create(CreateRouletteSessionRequest request, CancellationToken ct)
    {
        var user = UserFromToken();
        if (user is null) return Unauthorized(new { message = "انتهت صلاحية تسجيل الدخول. افتح مركز الألعاب مرة ثانية." });
        var scope = ValidateAndApplyTrustedScope(request);
        if (scope is not null) return scope;
        var result = await roulette.CreateSessionAsync(request, user, ct);
        return Result(result, request.GuildDiscordId, request.ChannelDiscordId, request.ActivityInstanceId);
    }

    [HttpGet("sessions/open")]
    public async Task<IActionResult> Open([FromQuery] string guildDiscordId, [FromQuery] string channelDiscordId, CancellationToken ct)
    {
        var userId = DiscordUserId();
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized(new { message = "انتهت صلاحية تسجيل الدخول. افتح مركز الألعاب مرة ثانية." });
        var scope = ValidateTrustedScope(guildDiscordId, channelDiscordId, null, out _);
        if (scope is not null) return scope;
        return Result(await roulette.GetOpenSessionsAsync(guildDiscordId, channelDiscordId, userId, ct), guildDiscordId, channelDiscordId, TrustedActivityInstanceId());
    }

    [HttpGet("sessions/my-active")]
    public async Task<IActionResult> MyActive([FromQuery] string guildDiscordId, [FromQuery] string channelDiscordId, CancellationToken ct)
    {
        var userId = DiscordUserId();
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized(new { message = "انتهت صلاحية تسجيل الدخول. افتح مركز الألعاب مرة ثانية." });
        var scope = ValidateTrustedScope(guildDiscordId, channelDiscordId, null, out _);
        if (scope is not null) return scope;
        return Result(await roulette.GetMyActiveSessionAsync(guildDiscordId, channelDiscordId, userId, ct), guildDiscordId, channelDiscordId, TrustedActivityInstanceId());
    }

    [HttpGet("sessions/{gameSessionId:guid}")]
    public async Task<IActionResult> Get(Guid gameSessionId, [FromQuery] string guildDiscordId, [FromQuery] string channelDiscordId, CancellationToken ct)
    {
        var userId = DiscordUserId();
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized(new { message = "انتهت صلاحية تسجيل الدخول. افتح مركز الألعاب مرة ثانية." });
        var scope = ValidateTrustedScope(guildDiscordId, channelDiscordId, null, out _);
        if (scope is not null) return scope;
        return Result(await roulette.GetSessionAsync(gameSessionId, guildDiscordId, channelDiscordId, userId, ct), guildDiscordId, channelDiscordId, TrustedActivityInstanceId());
    }

    [HttpPost("sessions/{gameSessionId:guid}/join")]
    public async Task<IActionResult> Join(Guid gameSessionId, RouletteScopeRequest request, CancellationToken ct)
    {
        var user = UserFromToken();
        if (user is null) return Unauthorized(new { message = "انتهت صلاحية تسجيل الدخول. افتح مركز الألعاب مرة ثانية." });
        var scope = ValidateAndApplyTrustedScope(request);
        if (scope is not null) return scope;
        return Result(await roulette.JoinSessionAsync(gameSessionId, request, user, ct), request.GuildDiscordId, request.ChannelDiscordId, request.ActivityInstanceId);
    }

    [HttpPost("sessions/{gameSessionId:guid}/leave")]
    public async Task<IActionResult> Leave(Guid gameSessionId, RouletteScopeRequest request, CancellationToken ct)
    {
        var userId = DiscordUserId();
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized(new { message = "انتهت صلاحية تسجيل الدخول. افتح مركز الألعاب مرة ثانية." });
        var scope = ValidateAndApplyTrustedScope(request);
        if (scope is not null) return scope;
        return Result(await roulette.LeaveSessionAsync(gameSessionId, request, userId, ct), request.GuildDiscordId, request.ChannelDiscordId, request.ActivityInstanceId);
    }

    [HttpPost("sessions/{gameSessionId:guid}/rounds/start")]
    public async Task<IActionResult> Start(Guid gameSessionId, RouletteScopeRequest request, CancellationToken ct)
    {
        var userId = DiscordUserId();
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized(new { message = "انتهت صلاحية تسجيل الدخول. افتح مركز الألعاب مرة ثانية." });
        var scope = ValidateAndApplyTrustedScope(request);
        if (scope is not null) return scope;
        return Result(await roulette.StartSessionAsync(gameSessionId, request, userId, ct), request.GuildDiscordId, request.ChannelDiscordId, request.ActivityInstanceId);
    }

    [HttpPost("sessions/{gameSessionId:guid}/spin")]
    public async Task<IActionResult> Spin(Guid gameSessionId, RouletteScopeRequest request, CancellationToken ct)
    {
        var userId = DiscordUserId();
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized(new { message = "انتهت صلاحية تسجيل الدخول. افتح مركز الألعاب مرة ثانية." });
        var scope = ValidateAndApplyTrustedScope(request);
        if (scope is not null) return scope;
        return Result(await roulette.SpinAsync(gameSessionId, request, userId, ct), request.GuildDiscordId, request.ChannelDiscordId, request.ActivityInstanceId);
    }

    [HttpPost("sessions/{gameSessionId:guid}/resolve-pending-action")]
    public async Task<IActionResult> Resolve(Guid gameSessionId, RouletteScopeRequest request, CancellationToken ct)
    {
        var userId = DiscordUserId();
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized(new { message = "انتهت صلاحية تسجيل الدخول. افتح مركز الألعاب مرة ثانية." });
        var scope = ValidateAndApplyTrustedScope(request);
        if (scope is not null) return scope;
        return Result(await roulette.ResolvePendingActionAsync(gameSessionId, request, userId, ct), request.GuildDiscordId, request.ChannelDiscordId, request.ActivityInstanceId);
    }

    [HttpPost("sessions/{gameSessionId:guid}/reconnect")]
    public async Task<IActionResult> Reconnect(Guid gameSessionId, RouletteScopeRequest request, CancellationToken ct)
    {
        var user = UserFromToken();
        if (user is null) return Unauthorized(new { message = "انتهت صلاحية تسجيل الدخول. افتح مركز الألعاب مرة ثانية." });
        var scope = ValidateAndApplyTrustedScope(request);
        if (scope is not null) return scope;
        return Result(await roulette.ReconnectAsync(gameSessionId, request, user, ct), request.GuildDiscordId, request.ChannelDiscordId, request.ActivityInstanceId);
    }

    [HttpPost("sessions/{gameSessionId:guid}/bets")]
    public async Task<IActionResult> Bet(Guid gameSessionId, PlaceRouletteBetRequest request, CancellationToken ct)
    {
        var userId = DiscordUserId();
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized(new { message = "انتهت صلاحية تسجيل الدخول. افتح مركز الألعاب مرة ثانية." });
        var scope = ValidateAndApplyTrustedScope(request);
        if (scope is not null) return scope;
        return Result(await roulette.PlaceBetAsync(gameSessionId, request, userId, ct), request.GuildDiscordId, request.ChannelDiscordId, request.ActivityInstanceId);
    }

    [HttpPost("sessions/{gameSessionId:guid}/use-power-up")]
    public IActionResult UsePowerUp(Guid gameSessionId)
    {
        return StatusCode(501, new
        {
            code = "feature_not_available",
            message = "الخصائص والمتجر غير متاحة على runtime الروليت الجديد حتى يكتمل نقل المحفظة الآمن.",
            feature = "roulette_power_ups"
        });
    }

    private TrustedDiscordUser? UserFromToken()
    {
        var id = DiscordUserId();
        if (string.IsNullOrWhiteSpace(id)) return null;
        return new TrustedDiscordUser
        {
            DiscordUserId = id,
            Username = User.FindFirst("username")?.Value ?? id,
            AvatarUrl = User.FindFirst("avatar_url")?.Value,
            DiscordGuildId = User.FindFirst("discord_guild_id")?.Value,
            DiscordChannelId = User.FindFirst("discord_channel_id")?.Value,
            ActivityInstanceId = User.FindFirst("activity_instance_id")?.Value
        };
    }

    private string? DiscordUserId() => User.FindFirst("discord_user_id")?.Value;
    private string? TrustedGuildId() => User.FindFirst("discord_guild_id")?.Value;
    private string? TrustedChannelId() => User.FindFirst("discord_channel_id")?.Value;
    private string? TrustedActivityInstanceId() => User.FindFirst("activity_instance_id")?.Value;

    private IActionResult? ValidateAndApplyTrustedScope(RouletteScopeRequest request)
    {
        var result = ValidateTrustedScope(request.GuildDiscordId, request.ChannelDiscordId, request.ActivityInstanceId, out var trustedActivityInstanceId);
        if (result is not null) return result;
        request.ActivityInstanceId = trustedActivityInstanceId;
        return null;
    }

    private IActionResult? ValidateAndApplyTrustedScope(CreateRouletteSessionRequest request)
    {
        var result = ValidateTrustedScope(request.GuildDiscordId, request.ChannelDiscordId, request.ActivityInstanceId, out var trustedActivityInstanceId);
        if (result is not null) return result;
        request.ActivityInstanceId = trustedActivityInstanceId;
        return null;
    }

    private IActionResult? ValidateTrustedScope(string requestedGuildId, string requestedChannelId, string? requestedActivityInstanceId, out string? trustedActivityInstanceId)
    {
        var trustedGuildId = TrustedGuildId();
        var trustedChannelId = TrustedChannelId();
        trustedActivityInstanceId = TrustedActivityInstanceId();

        if (string.IsNullOrWhiteSpace(trustedGuildId) || string.IsNullOrWhiteSpace(trustedChannelId))
        {
            LogForbiddenDecision(requestedGuildId, requestedChannelId, trustedActivityInstanceId, "missing_trusted_guild_or_channel_claims");
            return StatusCode(403, new { message = "جلسة Discord Activity غير موثوقة. افتح مركز الألعاب مرة ثانية." });
        }

        if (!string.Equals(trustedGuildId, requestedGuildId, StringComparison.Ordinal) || !string.Equals(trustedChannelId, requestedChannelId, StringComparison.Ordinal))
        {
            LogForbiddenDecision(requestedGuildId, requestedChannelId, trustedActivityInstanceId, "trusted_guild_or_channel_mismatch");
            return StatusCode(403, new { message = "لا يمكن استخدام جلسة الألعاب خارج الروم الذي فُتحت منه." });
        }

        if (string.IsNullOrWhiteSpace(trustedActivityInstanceId))
        {
            if (environment.IsDevelopment() && authOptions.Value.AllowMissingActivityInstanceInDevelopment)
            {
                logger.LogWarning(
                    "Roulette trusted context allowed without activity instance in Development. Endpoint={Endpoint}, UserDiscordId={DiscordUserId}, GuildId={GuildId}, ChannelId={ChannelId}, ActivityInstancePresent={ActivityInstancePresent}, DenialReason={DenialReason}, CorrelationId={CorrelationId}.",
                    Request.Path.Value,
                    DiscordUserId(),
                    trustedGuildId,
                    trustedChannelId,
                    false,
                    "missing_activity_instance_claim_development_override",
                    CorrelationId());
                return null;
            }

            LogForbiddenDecision(requestedGuildId, requestedChannelId, trustedActivityInstanceId, "missing_activity_instance_claim");
            return StatusCode(403, new { message = "جلسة Discord Activity غير مكتملة. افتح مركز الألعاب مرة ثانية." });
        }

        if (!string.IsNullOrWhiteSpace(requestedActivityInstanceId) && !string.Equals(trustedActivityInstanceId, requestedActivityInstanceId, StringComparison.Ordinal))
        {
            LogForbiddenDecision(requestedGuildId, requestedChannelId, trustedActivityInstanceId, "activity_instance_mismatch");
            return StatusCode(403, new { message = "لا يمكن استخدام هذه الجلسة من Activity مختلف." });
        }

        return null;
    }

    private ObjectResult Result<T>(DiscordBot.Shared.OperationResult<T> result, string? guildDiscordId = null, string? channelDiscordId = null, string? activityInstanceId = null)
    {
        if (!result.Succeeded && result.StatusCode == 403)
        {
            LogForbiddenDecision(guildDiscordId, channelDiscordId, activityInstanceId, result.Code ?? result.Error ?? "operation_forbidden");
        }

        return StatusCode(result.StatusCode, result.Succeeded ? result.Value : new { code = result.Code, message = result.Error, feature = result.Feature });
    }

    private void LogForbiddenDecision(string? guildDiscordId, string? channelDiscordId, string? activityInstanceId, string denialReason)
    {
        logger.LogWarning(
            "Roulette request forbidden. Endpoint={Endpoint}, UserDiscordId={DiscordUserId}, GuildId={GuildId}, ChannelId={ChannelId}, ActivityInstancePresent={ActivityInstancePresent}, DenialReason={DenialReason}, CorrelationId={CorrelationId}.",
            Request.Path.Value,
            DiscordUserId(),
            guildDiscordId,
            channelDiscordId,
            !string.IsNullOrWhiteSpace(activityInstanceId),
            denialReason,
            CorrelationId());
    }

    private string CorrelationId() =>
        Request.Headers.TryGetValue("X-Correlation-ID", out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.ToString()
            : HttpContext.TraceIdentifier;
}
