using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DiscordBot.Activities.Api.Controllers;
using DiscordBot.Activities.Api.Options;
using DiscordBot.Activities.Application.Abstractions;
using DiscordBot.Activities.Application.Models;
using DiscordBot.Activities.Infrastructure.Auth;
using DiscordBot.Activities.Infrastructure.Options;
using DiscordBot.Shared;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace DiscordBot.Activities.IntegrationTests;

public class RouletteAuthorizationTests
{
    private const string UserId = "123456789012345678";
    private const string GuildId = "111111111111111111";
    private const string ChannelId = "222222222222222222";
    private const string ActivityInstanceId = "activity-instance-a";

    [Fact]
    public void Activity_token_includes_trusted_discord_context_claims_when_instance_is_present()
    {
        var service = new ActivityTokenService(Options.Create(new ActivitiesJwtOptions
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            SigningKey = ActivitiesApiFactory.SigningKey
        }));

        var response = service.CreateToken(new TrustedDiscordUser
        {
            DiscordUserId = UserId,
            Username = "Tester",
            DiscordGuildId = GuildId,
            DiscordChannelId = ChannelId,
            ActivityInstanceId = ActivityInstanceId
        });

        var claims = new JwtSecurityTokenHandler().ReadJwtToken(response.AccessToken).Claims.ToDictionary(x => x.Type, x => x.Value);
        claims["discord_user_id"].Should().Be(UserId);
        claims["discord_guild_id"].Should().Be(GuildId);
        claims["discord_channel_id"].Should().Be(ChannelId);
        claims["activity_instance_id"].Should().Be(ActivityInstanceId);
    }

    [Fact]
    public void Activity_token_omits_activity_instance_claim_when_instance_is_missing()
    {
        var service = new ActivityTokenService(Options.Create(new ActivitiesJwtOptions
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            SigningKey = ActivitiesApiFactory.SigningKey
        }));

        var response = service.CreateToken(new TrustedDiscordUser
        {
            DiscordUserId = UserId,
            Username = "Tester",
            DiscordGuildId = GuildId,
            DiscordChannelId = ChannelId
        });

        new JwtSecurityTokenHandler().ReadJwtToken(response.AccessToken).Claims
            .Should().NotContain(x => x.Type == "activity_instance_id");
    }

    [Fact]
    public async Task Open_sessions_with_missing_activity_instance_claim_is_rejected()
    {
        var controller = Controller(activityInstanceId: null);

        var result = await controller.Open(GuildId, ChannelId, CancellationToken.None);

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task Open_sessions_with_valid_trusted_context_reaches_runtime()
    {
        var runtime = new FakeRouletteRuntimeService();
        runtime.OpenResult = OperationResult<IReadOnlyList<RouletteSessionDto>>.Ok([]);
        var controller = Controller(runtime: runtime);

        var result = await controller.Open(GuildId, ChannelId, CancellationToken.None);

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(200);
        runtime.OpenCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Create_session_with_valid_trusted_context_applies_activity_instance_and_reaches_runtime()
    {
        var runtime = new FakeRouletteRuntimeService();
        runtime.CreateResult = OperationResult<RouletteSessionDto>.Ok(new RouletteSessionDto());
        var controller = Controller(runtime: runtime);
        var request = new CreateRouletteSessionRequest { GuildDiscordId = GuildId, ChannelDiscordId = ChannelId };

        var result = await controller.Create(request, CancellationToken.None);

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(200);
        runtime.CreateCalled.Should().BeTrue();
        request.ActivityInstanceId.Should().Be(ActivityInstanceId);
    }

    [Fact]
    public async Task Create_session_with_mismatched_activity_instance_is_rejected_before_runtime()
    {
        var runtime = new FakeRouletteRuntimeService();
        var controller = Controller(runtime: runtime);
        var request = new CreateRouletteSessionRequest { GuildDiscordId = GuildId, ChannelDiscordId = ChannelId, ActivityInstanceId = "different-instance" };

        var result = await controller.Create(request, CancellationToken.None);

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(403);
        runtime.CreateCalled.Should().BeFalse();
    }

    private static RouletteController Controller(FakeRouletteRuntimeService? runtime = null, string? activityInstanceId = ActivityInstanceId)
    {
        var controller = new RouletteController(
            runtime ?? new FakeRouletteRuntimeService(),
            Options.Create(new ActivityRuntimeAuthOptions { AllowMissingActivityInstanceInDevelopment = false }),
            new FakeEnvironment(),
            NullLogger<RouletteController>.Instance);

        var claims = new List<Claim>
        {
            new("discord_user_id", UserId),
            new("username", "Tester"),
            new("discord_guild_id", GuildId),
            new("discord_channel_id", ChannelId)
        };
        if (!string.IsNullOrWhiteSpace(activityInstanceId))
        {
            claims.Add(new Claim("activity_instance_id", activityInstanceId));
        }

        var http = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")) };
        http.Request.Headers["X-Correlation-ID"] = "test-correlation";
        controller.ControllerContext = new ControllerContext { HttpContext = http };
        return controller;
    }

    private sealed class FakeRouletteRuntimeService : IRouletteRuntimeService
    {
        public bool CreateCalled { get; private set; }
        public bool OpenCalled { get; private set; }
        public OperationResult<RouletteSessionDto> CreateResult { get; set; } = OperationResult<RouletteSessionDto>.Ok(new RouletteSessionDto());
        public OperationResult<IReadOnlyList<RouletteSessionDto>> OpenResult { get; set; } = OperationResult<IReadOnlyList<RouletteSessionDto>>.Ok([]);

        public Task<OperationResult<RouletteSessionDto>> CreateSessionAsync(CreateRouletteSessionRequest request, TrustedDiscordUser user, CancellationToken ct = default)
        {
            CreateCalled = true;
            return Task.FromResult(CreateResult);
        }

        public Task<OperationResult<IReadOnlyList<RouletteSessionDto>>> GetOpenSessionsAsync(string guildDiscordId, string channelDiscordId, string userDiscordId, CancellationToken ct = default)
        {
            OpenCalled = true;
            return Task.FromResult(OpenResult);
        }

        public Task<OperationResult<MyActiveRouletteSessionDto>> GetMyActiveSessionAsync(string guildDiscordId, string channelDiscordId, string userDiscordId, CancellationToken ct = default) => Task.FromResult(OperationResult<MyActiveRouletteSessionDto>.Ok(new MyActiveRouletteSessionDto()));
        public Task<OperationResult<RouletteSessionDto>> GetSessionAsync(Guid gameSessionId, string guildDiscordId, string channelDiscordId, string userDiscordId, CancellationToken ct = default) => Task.FromResult(OperationResult<RouletteSessionDto>.Ok(new RouletteSessionDto()));
        public Task<OperationResult<RouletteSessionDto>> JoinSessionAsync(Guid gameSessionId, RouletteScopeRequest request, TrustedDiscordUser user, CancellationToken ct = default) => Task.FromResult(OperationResult<RouletteSessionDto>.Ok(new RouletteSessionDto()));
        public Task<OperationResult<RouletteSessionDto>> LeaveSessionAsync(Guid gameSessionId, RouletteScopeRequest request, string userDiscordId, CancellationToken ct = default) => Task.FromResult(OperationResult<RouletteSessionDto>.Ok(new RouletteSessionDto()));
        public Task<OperationResult<RouletteSessionDto>> StartSessionAsync(Guid gameSessionId, RouletteScopeRequest request, string userDiscordId, CancellationToken ct = default) => Task.FromResult(OperationResult<RouletteSessionDto>.Ok(new RouletteSessionDto()));
        public Task<OperationResult<RouletteSpinResultDto>> SpinAsync(Guid gameSessionId, RouletteScopeRequest request, string userDiscordId, CancellationToken ct = default) => Task.FromResult(OperationResult<RouletteSpinResultDto>.Ok(new RouletteSpinResultDto()));
        public Task<OperationResult<RouletteSessionDto>> ResolvePendingActionAsync(Guid gameSessionId, RouletteScopeRequest request, string userDiscordId, CancellationToken ct = default) => Task.FromResult(OperationResult<RouletteSessionDto>.Ok(new RouletteSessionDto()));
        public Task<OperationResult<RouletteSessionDto>> ReconnectAsync(Guid gameSessionId, RouletteScopeRequest request, TrustedDiscordUser user, CancellationToken ct = default) => Task.FromResult(OperationResult<RouletteSessionDto>.Ok(new RouletteSessionDto()));
        public Task<OperationResult<RouletteBetDto>> PlaceBetAsync(Guid gameSessionId, PlaceRouletteBetRequest request, string userDiscordId, CancellationToken ct = default) => Task.FromResult(OperationResult<RouletteBetDto>.Ok(new RouletteBetDto()));
    }

    private sealed class FakeEnvironment : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Production";
        public string ApplicationName { get; set; } = "Tests";
        public string WebRootPath { get; set; } = "";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
