using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace DiscordBot.Activities.IntegrationTests;

public class ActivitiesAuthTests(ActivitiesApiFactory factory) : IClassFixture<ActivitiesApiFactory>
{
    [Fact]
    public async Task Roulette_endpoint_without_jwt_is_rejected()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/roulette/sessions/open?guildDiscordId=1&channelDiscordId=2");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Roulette_endpoint_with_invalid_jwt_is_rejected()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-valid-token");

        var response = await client.GetAsync("/api/roulette/sessions/open?guildDiscordId=1&channelDiscordId=2");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SignalR_negotiate_without_jwt_is_rejected()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsync("/hubs/games/negotiate?negotiateVersion=1", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Power_up_endpoint_returns_structured_feature_unavailable_response()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token("123456789", "Tester"));

        var response = await client.PostAsync($"/api/roulette/sessions/{Guid.NewGuid()}/use-power-up", null);
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.NotImplemented);
        body.Should().Contain("feature_not_available");
        body.Should().Contain("roulette_power_ups");
    }

    [Fact]
    public async Task Roulette_capabilities_show_pilot_runtime_features()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token("123456789", "Tester"));

        var response = await client.GetAsync("/api/roulette/capabilities");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().Contain("activities-v1");
        body.Should().Contain("\"supportsPowerUps\":false");
        body.Should().Contain("\"supportsReconnect\":true");
    }

    [Fact]
    public async Task Pilot_diagnostics_requires_service_key()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/internal/diagnostics/pilot");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Roulette_scope_request_with_wrong_guild_is_rejected_before_runtime()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token("123456789", "Tester", "111111111111111111", "222222222222222222", "activity-instance-a"));

        var response = await client.PostAsync($"/api/roulette/sessions/{Guid.NewGuid()}/join", Json(new { guildDiscordId = "999999999999999999", channelDiscordId = "222222222222222222", activityInstanceId = "activity-instance-a" }));
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        body.Should().Contain("خارج الروم");
    }

    [Fact]
    public async Task Roulette_scope_request_with_wrong_activity_instance_is_rejected_before_runtime()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token("123456789", "Tester", "111111111111111111", "222222222222222222", "activity-instance-a"));

        var response = await client.PostAsync($"/api/roulette/sessions/{Guid.NewGuid()}/join", Json(new { guildDiscordId = "111111111111111111", channelDiscordId = "222222222222222222", activityInstanceId = "activity-instance-b" }));
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        body.Should().Contain("Activity مختلف");
    }

    [Fact]
    public async Task Roulette_scope_request_without_activity_instance_claim_is_rejected_by_default()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token("123456789", "Tester", "111111111111111111", "222222222222222222"));

        var response = await client.PostAsync($"/api/roulette/sessions/{Guid.NewGuid()}/join", Json(new { guildDiscordId = "111111111111111111", channelDiscordId = "222222222222222222" }));
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        body.Should().Contain("غير مكتملة");
    }

    private static StringContent Json(object value) => new(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json");

    private static string Token(string discordUserId, string username, string? guildDiscordId = null, string? channelDiscordId = null, string? activityInstanceId = null)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ActivitiesApiFactory.SigningKey));
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, discordUserId),
            new("discord_user_id", discordUserId),
            new("username", username)
        };
        if (!string.IsNullOrWhiteSpace(guildDiscordId)) claims.Add(new Claim("discord_guild_id", guildDiscordId));
        if (!string.IsNullOrWhiteSpace(channelDiscordId)) claims.Add(new Claim("discord_channel_id", channelDiscordId));
        if (!string.IsNullOrWhiteSpace(activityInstanceId)) claims.Add(new Claim("activity_instance_id", activityInstanceId));

        var token = new JwtSecurityToken(
            issuer: "DiscordBot.Activities.Tests",
            audience: "DiscordBot.Activity.Tests",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
