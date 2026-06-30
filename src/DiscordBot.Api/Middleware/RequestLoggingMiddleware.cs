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

        await _next(context);

        var statusCode = context.Response.StatusCode;
        var level = statusCode >= 500 ? LogLevel.Error
            : statusCode >= 400 ? LogLevel.Warning
            : LogLevel.Information;

        _logger.Log(level, "{Method} {Path} responded {StatusCode}", method, path, statusCode);
    }
}
