using DiscordBot.Api.Extensions;
using DiscordBot.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DiscordBot.Api.Filters;

/// <summary>
/// Requires a logged-in Discord user who exists in PlatformAdmins.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class PlatformAdminAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.HttpContext.User.Identity?.IsAuthenticated != true)
        {
            context.Result = new UnauthorizedObjectResult(new { message = "Authentication required." });
            return;
        }

        var discordUserId = context.HttpContext.User.GetDiscordUserId();
        if (string.IsNullOrWhiteSpace(discordUserId))
        {
            context.Result = new UnauthorizedObjectResult(new { message = "Missing Discord user id." });
            return;
        }

        var adminService = context.HttpContext.RequestServices.GetRequiredService<IPlatformAdminService>();
        var isAdmin = await adminService.IsAdminAsync(discordUserId, context.HttpContext.RequestAborted);

        if (!isAdmin)
        {
            context.Result = new ForbidResult();
            return;
        }

        await next();
    }
}
