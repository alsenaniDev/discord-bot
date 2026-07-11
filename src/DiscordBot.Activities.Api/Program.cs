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
    CheckRequired(errors, configuration.GetConnectionString("ActivitiesDatabase") ?? configuration.GetConnectionString("DefaultConnection"), "ConnectionStrings:ActivitiesDatabase");
    CheckRequired(errors, configuration["Discord:ClientId"], "Discord:ClientId");
    CheckRequired(errors, configuration["Discord:ClientSecret"], "Discord:ClientSecret");
    CheckRequired(errors, configuration["Discord:RedirectUri"], "Discord:RedirectUri");
    CheckRequired(errors, configuration["PlatformApi:BaseUrl"], "PlatformApi:BaseUrl");
    CheckRequired(errors, configuration["PlatformApi:ServiceToken"], "PlatformApi:ServiceToken");
    CheckRequired(errors, configuration["ActivitiesDiagnostics:ServiceToken"], "ActivitiesDiagnostics:ServiceToken");
    CheckRequired(errors, configuration["Jwt:Issuer"], "Jwt:Issuer");
    CheckRequired(errors, configuration["Jwt:Audience"], "Jwt:Audience");
    CheckRequired(errors, configuration["Jwt:SigningKey"], "Jwt:SigningKey");

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

static void CheckRequired(List<string> errors, string? value, string key)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        errors.Add($"{key} must be configured.");
    }
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
