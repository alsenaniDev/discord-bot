using DiscordBot.Infrastructure.Options;

namespace DiscordBot.Api.Extensions;

public static class ConfigurationValidationExtensions
{
    private static readonly string[] PlaceholderFragments =
    [
        "YOUR_",
        "CHANGE_ME",
        "your-domain.com",
        "REPLACE_WITH"
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

        var discord = configuration.GetSection(DiscordOptions.SectionName).Get<DiscordOptions>() ?? new DiscordOptions();
        CheckSetting(errors, "Discord:ClientId", discord.ClientId, strict);
        CheckSetting(errors, "Discord:ClientSecret", discord.ClientSecret, strict);
        CheckSetting(errors, "Discord:BotToken", discord.BotToken, strict);
        CheckSetting(errors, "Discord:RedirectUri", discord.RedirectUri, strict);
        CheckSetting(errors, "Discord:DashboardUrl", discord.DashboardUrl, strict);

        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        if (string.IsNullOrWhiteSpace(jwt.Secret) || jwt.Secret.Length < 32)
        {
            errors.Add("Jwt:Secret must be at least 32 characters.");
        }
        else if (strict && IsPlaceholder(jwt.Secret))
        {
            errors.Add("Jwt:Secret is still a placeholder value.");
        }

        var bot = configuration.GetSection(BotOptions.SectionName).Get<BotOptions>() ?? new BotOptions();
        CheckSetting(errors, "Bot:ApiKey", bot.ApiKey, strict);

        if (strict)
        {
            if (discord.RedirectUri.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("Discord:RedirectUri must use HTTPS in Production.");
            }

            if (discord.DashboardUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("Discord:DashboardUrl must use HTTPS in Production.");
            }
        }

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

    private static bool IsPlaceholder(string value)
    {
        return PlaceholderFragments.Any(fragment =>
            value.Contains(fragment, StringComparison.OrdinalIgnoreCase));
    }
}
