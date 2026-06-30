using System.Security.Claims;

namespace DiscordBot.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string? GetDiscordUserId(this ClaimsPrincipal user) =>
        user.FindFirstValue("discord_id");
}
