using DiscordBot.Infrastructure.Options;
using DiscordBot.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace DiscordBot.Api.Filters;

/// <summary>
/// Validates X-Bot-Api-Key header for internal bot endpoints.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class BotApiKeyAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var botOptions = context.HttpContext.RequestServices
            .GetRequiredService<IOptions<BotOptions>>().Value;

        if (string.IsNullOrWhiteSpace(botOptions.ApiKey))
        {
            context.Result = new UnauthorizedObjectResult(new { message = "Bot API key is not configured." });
            return;
        }

        if (!context.HttpContext.Request.Headers.TryGetValue("X-Bot-Api-Key", out var providedKey)
            || providedKey != botOptions.ApiKey)
        {
            context.Result = new UnauthorizedObjectResult(new { message = "Invalid bot API key." });
            return;
        }

        await next();
    }
}
