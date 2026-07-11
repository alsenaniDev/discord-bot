using System.Security.Claims;
using System.Text;
using DiscordBot.Activities.Api.Health;
using DiscordBot.Activities.Api.Hubs;
using DiscordBot.Activities.Api.Options;
using DiscordBot.Activities.Application.Abstractions;
using DiscordBot.Activities.Infrastructure;
using DiscordBot.Activities.Infrastructure.Data;
using DiscordBot.Activities.Infrastructure.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Local overrides (gitignored). Re-apply env vars afterward so CLI/hosting overrides win.
builder.Configuration.AddJsonFile(
    $"appsettings.{builder.Environment.EnvironmentName}.local.json",
    optional: true,
    reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();

var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddScoped<IRouletteRealtimePublisher, RouletteRealtimePublisher>();
builder.Services.AddHealthChecks()
    .AddCheck<ActivitiesDatabaseHealthCheck>("activities-db", tags: ["live"])
    .AddCheck<ActivitiesReadinessHealthCheck>("activities-ready", tags: ["ready"]);
builder.Services.AddActivitiesInfrastructure(builder.Configuration);
builder.Services.Configure<ActivitiesCorsOptions>(builder.Configuration.GetSection(ActivitiesCorsOptions.SectionName));
builder.Services.Configure<ActivityRuntimeAuthOptions>(builder.Configuration.GetSection(ActivityRuntimeAuthOptions.SectionName));
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("activity-api", limiter =>
    {
        limiter.PermitLimit = 120;
        limiter.Window = TimeSpan.FromMinutes(1);
    });
});

var jwt = builder.Configuration.GetSection(ActivitiesJwtOptions.SectionName).Get<ActivitiesJwtOptions>() ?? new ActivitiesJwtOptions();
if (string.IsNullOrWhiteSpace(jwt.SigningKey) || jwt.SigningKey.Length < 32)
{
    throw new InvalidOperationException("Jwt:SigningKey must be configured and at least 32 characters.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            NameClaimType = "discord_user_id",
            RoleClaimType = ClaimTypes.Role,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrWhiteSpace(accessToken) && context.HttpContext.Request.Path.StartsWithSegments("/hubs/games"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

var cors = builder.Configuration.GetSection(ActivitiesCorsOptions.SectionName).Get<ActivitiesCorsOptions>() ?? new ActivitiesCorsOptions();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Activities", policy =>
    {
        var origins = cors.AllowedOrigins.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim().TrimEnd('/')).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (origins.Length > 0) policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
    });
});

var app = builder.Build();

LogSafeStartupConfiguration(app, builder.Configuration, cors, jwt);
ValidateActivitiesConfiguration(app, builder.Configuration);

app.UseHttpsRedirection();
app.UseCors("Activities");
app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers.TryGetValue("X-Correlation-ID", out var header) && !string.IsNullOrWhiteSpace(header)
        ? header.ToString()
        : Guid.NewGuid().ToString("N");
    context.Response.Headers["X-Correlation-ID"] = correlationId;

    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    try
    {
        await next(context);
    }
    finally
    {
        stopwatch.Stop();
        var userId = context.User.FindFirst("discord_user_id")?.Value;
        var level = context.Response.StatusCode >= 500 ? LogLevel.Error
            : context.Response.StatusCode >= 400 ? LogLevel.Warning
            : LogLevel.Information;
        app.Logger.Log(
            level,
            "Activities API request completed. CorrelationId={CorrelationId}, Method={Method}, Path={Path}, StatusCode={StatusCode}, Origin={Origin}, UserDiscordId={UserDiscordId}, DurationMs={DurationMs}",
            correlationId,
            context.Request.Method,
            context.Request.Path.Value,
            context.Response.StatusCode,
            context.Request.Headers.Origin.ToString(),
            userId,
            stopwatch.ElapsedMilliseconds);
    }
});
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers().RequireRateLimiting("activity-api");
app.MapHub<GameHub>("/hubs/games");
app.MapHealthChecks("/health");
app.MapGet("/health/live", () => Results.Ok(new { status = "Healthy" }));
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

app.Run();

static void LogSafeStartupConfiguration(WebApplication app, IConfiguration configuration, ActivitiesCorsOptions cors, ActivitiesJwtOptions jwt)
{
    app.Logger.LogInformation(
        "Activities API configuration loaded. Environment={Environment}, Database={Database}, PlatformApiBaseUrl={PlatformApiBaseUrl}, CorsOrigins={CorsOrigins}, JwtIssuer={JwtIssuer}, JwtAudience={JwtAudience}",
        app.Environment.EnvironmentName,
        SafeConnectionStringSummary(configuration.GetConnectionString("ActivitiesDatabase") ?? configuration.GetConnectionString("DefaultConnection")),
        configuration["PlatformApi:BaseUrl"],
        string.Join(", ", cors.AllowedOrigins.Where(x => !string.IsNullOrWhiteSpace(x))),
        jwt.Issuer,
        jwt.Audience);
}

static void ValidateActivitiesConfiguration(WebApplication app, IConfiguration configuration)
{
    var errors = new List<string>();
    var strict = app.Environment.IsProduction();
    CheckRequired(errors, configuration.GetConnectionString("ActivitiesDatabase") ?? configuration.GetConnectionString("DefaultConnection"), "ConnectionStrings:ActivitiesDatabase", strict);
    CheckRequired(errors, configuration["Discord:ClientId"], "Discord:ClientId", strict);
    CheckRequired(errors, configuration["Discord:ClientSecret"], "Discord:ClientSecret", strict);
    CheckRequired(errors, configuration["Discord:RedirectUri"], "Discord:RedirectUri", strict);
    CheckRequired(errors, configuration["PlatformApi:BaseUrl"], "PlatformApi:BaseUrl", strict);
    CheckRequired(errors, configuration["PlatformApi:ServiceToken"], "PlatformApi:ServiceToken", strict);
    CheckRequired(errors, configuration["ActivitiesDiagnostics:ServiceToken"], "ActivitiesDiagnostics:ServiceToken", strict);
    CheckRequired(errors, configuration["Jwt:Issuer"], "Jwt:Issuer", strict);
    CheckRequired(errors, configuration["Jwt:Audience"], "Jwt:Audience", strict);
    CheckRequired(errors, configuration["Jwt:SigningKey"], "Jwt:SigningKey", strict);

    if (strict)
    {
        CheckProductionUrl(errors, "Discord:RedirectUri", configuration["Discord:RedirectUri"]);
        CheckProductionUrl(errors, "PlatformApi:BaseUrl", configuration["PlatformApi:BaseUrl"]);
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        if (origins.Length == 0)
        {
            errors.Add("Cors:AllowedOrigins must include at least one explicit production origin.");
        }
        foreach (var origin in origins.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            if (origin.Trim() == "*")
            {
                errors.Add("Cors:AllowedOrigins must not use wildcard origins with credentials.");
            }
            CheckProductionUrl(errors, "Cors:AllowedOrigins", origin);
        }
    }

    if (errors.Count == 0)
    {
        return;
    }

    var message = "Activities API configuration issues detected:"
        + Environment.NewLine
        + string.Join(Environment.NewLine, errors.Select(e => $"  - {e}"));

    if (app.Environment.IsProduction())
    {
        app.Logger.LogCritical("{Message}", message);
        throw new InvalidOperationException(message);
    }

    app.Logger.LogWarning("{Message}", message);
}

static void CheckRequired(List<string> errors, string? value, string key, bool strict)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        errors.Add($"{key} must be configured.");
        return;
    }

    if (strict && IsPlaceholder(value))
    {
        errors.Add($"{key} is still a placeholder value.");
    }
}

static void CheckProductionUrl(List<string> errors, string key, string? value)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        return;
    }

    if (value.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
    {
        errors.Add($"{key} must use HTTPS in Production.");
    }

    if (value.Contains("localhost", StringComparison.OrdinalIgnoreCase) || value.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase))
    {
        errors.Add($"{key} must not use localhost in Production.");
    }
}

static bool IsPlaceholder(string value)
{
    string[] fragments = ["YOUR_", "CHANGE_ME", "REPLACE_WITH", "your-domain.com", "example.com"];
    return fragments.Any(fragment => value.Contains(fragment, StringComparison.OrdinalIgnoreCase));
}

static string SafeConnectionStringSummary(string? connectionString)
{
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        return "(missing)";
    }

    var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(part => part.Split('=', 2, StringSplitOptions.TrimEntries))
        .Where(part => part.Length == 2)
        .ToDictionary(part => part[0], part => part[1], StringComparer.OrdinalIgnoreCase);

    parts.TryGetValue("Host", out var host);
    parts.TryGetValue("Port", out var port);
    parts.TryGetValue("Database", out var database);
    return $"Host={host ?? "unknown"};Port={port ?? "unknown"};Database={database ?? "unknown"}";
}

public partial class Program { }
