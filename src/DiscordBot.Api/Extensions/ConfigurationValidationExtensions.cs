using DiscordBot.Infrastructure.Options;

namespace DiscordBot.Api.Extensions;

public static class ConfigurationValidationExtensions
{
    private static readonly string[] PlaceholderFragments =
    [
        "YOUR_",
        "CHANGE_ME",
        "REPLACE_WITH",
        "your-domain.com"
    ];

    public static void ValidateRequiredConfiguration(this WebApplication app)
    {
        var errors = CollectErrors(app.Configuration, app.Environment, strict: app.Environment.IsProduction());

        if (errors.Count == 0)
        {
            var dashboardUrl = app.Configuration.GetSection(DiscordOptions.SectionName).Get<DiscordOptions>()?.DashboardUrl;
            app.Logger.LogInformation(
                "Configuration validated. Environment={Environment}, DashboardUrl={DashboardUrl}",
                app.Environment.EnvironmentName,
                dashboardUrl);
            return;
        }

        var message = "API configuration issues detected:"
            + Environment.NewLine
            + string.Join(Environment.NewLine, errors.Select(e => $"  - {e}"));

        if (app.Environment.IsProduction())
        {
            app.Logger.LogCritical("{Message}", message);
            throw new InvalidOperationException(message);
        }

        app.Logger.LogWarning("{Message}", message);
    }

    private static List<string> CollectErrors(IConfiguration configuration, IHostEnvironment environment, bool strict)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(configuration.GetConnectionString("DefaultConnection")))
        {
            errors.Add("ConnectionStrings:DefaultConnection is missing.");
        }
        else if (strict && IsPlaceholder(configuration.GetConnectionString("DefaultConnection")!))
        {
            errors.Add("ConnectionStrings:DefaultConnection is still a placeholder value.");
        }

        var discord = configuration.GetSection(DiscordOptions.SectionName).Get<DiscordOptions>() ?? new DiscordOptions();
        CheckSetting(errors, "Discord:ClientId", discord.ClientId, strict);
        CheckSetting(errors, "Discord:ClientSecret", discord.ClientSecret, strict);
        CheckSetting(errors, "Discord:BotToken", discord.BotToken, strict);
        CheckSetting(errors, "Discord:RedirectUri", discord.RedirectUri, strict);
        CheckSetting(errors, "Discord:DashboardUrl", discord.DashboardUrl, strict);

        if (strict)
        {
            CheckProductionUrl(errors, "Discord:RedirectUri", discord.RedirectUri);
            CheckProductionUrls(errors, "Discord:DashboardUrl", discord.DashboardUrl);
        }

        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        if (string.IsNullOrWhiteSpace(jwt.Secret) || jwt.Secret.Length < 32)
        {
            errors.Add("Jwt:Secret must be at least 32 characters.");
        }
        else if (strict && IsPlaceholder(jwt.Secret))
        {
            errors.Add("Jwt:Secret is still a placeholder value.");
        }

        CheckSetting(errors, "Jwt:Issuer", jwt.Issuer, strict);
        CheckSetting(errors, "Jwt:Audience", jwt.Audience, strict);

        var bot = configuration.GetSection(BotOptions.SectionName).Get<BotOptions>() ?? new BotOptions();
        CheckSetting(errors, "Bot:ApiKey", bot.ApiKey, strict);

        var admin = configuration.GetSection(AdminOptions.SectionName).Get<AdminOptions>() ?? new AdminOptions();
        CheckSetting(errors, "Admin:DiscordUserId", admin.DiscordUserId, strict);

        return errors;
    }

    private static void CheckSetting(List<string> errors, string key, string? value, bool strict)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{key} is missing.");
            return;
        }

        if (strict && IsPlaceholder(value))
        {
            errors.Add($"{key} is still a placeholder value.");
        }
    }

    private static void CheckProductionUrls(List<string> errors, string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        foreach (var origin in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            CheckProductionUrl(errors, key, origin);
        }
    }

    private static void CheckProductionUrl(List<string> errors, string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (value.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add($"{key} must use HTTPS in Production.");
        }

        if (IsLocalhost(value))
        {
            errors.Add($"{key} must not use localhost in Production.");
        }
    }

    private static bool IsPlaceholder(string value)
    {
        return PlaceholderFragments.Any(fragment =>
            value.Contains(fragment, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsLocalhost(string value)
    {
        return value.Contains("localhost", StringComparison.OrdinalIgnoreCase)
            || value.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase);
    }
}
