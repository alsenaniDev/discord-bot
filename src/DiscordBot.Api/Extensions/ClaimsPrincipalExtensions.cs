using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace DiscordBot.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string? GetDiscordUserId(this ClaimsPrincipal user) =>
        user.FindFirstValue("discord_id");

    public static Guid? GetUserId(this ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}
