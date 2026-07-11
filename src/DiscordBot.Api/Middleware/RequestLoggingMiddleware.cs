using System.Diagnostics;

namespace DiscordBot.Api.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var method = context.Request.Method;
        var path = context.Request.Path;
        var correlationId = context.Request.Headers.TryGetValue("X-Correlation-ID", out var header) && !string.IsNullOrWhiteSpace(header)
            ? header.ToString()
            : Guid.NewGuid().ToString("N");
        context.Response.Headers["X-Correlation-ID"] = correlationId;
        var sw = Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();

            var statusCode = context.Response.StatusCode;
            var level = statusCode >= 500 ? LogLevel.Error
                : statusCode >= 400 ? LogLevel.Warning
                : sw.ElapsedMilliseconds >= 1000 ? LogLevel.Warning
                : LogLevel.Information;

            _logger.Log(
                level,
                "{Method} {Path} responded {StatusCode} in {Elapsed}ms. CorrelationId={CorrelationId}, Origin={Origin}, UserDiscordId={UserDiscordId}",
                method,
                path,
                statusCode,
                sw.ElapsedMilliseconds,
                correlationId,
                context.Request.Headers.Origin.ToString(),
                context.User.FindFirst("discord_user_id")?.Value);
        }
    }
}
