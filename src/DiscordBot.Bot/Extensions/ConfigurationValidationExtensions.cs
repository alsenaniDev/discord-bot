using DiscordBot.Bot.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DiscordBot.Bot.Extensions;

public static class ConfigurationValidationExtensions
{
    private static readonly string[] PlaceholderFragments =
    [
        "YOUR_",
        "CHANGE_ME",
        "your-domain.com"
    ];

    public static void ValidateRequiredConfiguration(this IHost host)
    {
        var configuration = host.Services.GetRequiredService<IConfiguration>();
        var environment = host.Services.GetRequiredService<IHostEnvironment>();
        var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("DiscordBot.Bot.Startup");
        var strict = environment.IsProduction();

        var errors = CollectErrors(configuration, strict);

        if (errors.Count == 0)
        {
            var api = configuration.GetSection(ApiOptions.SectionName).Get<ApiOptions>();
            var platform = configuration.GetSection(PlatformOptions.SectionName).Get<PlatformOptions>();
            logger.LogInformation(
                "Bot configuration validated. Environment={Environment}, ApiBaseUrl={ApiBaseUrl}, DashboardUrl={DashboardUrl}",
                environment.EnvironmentName,
                api?.BaseUrl,
                platform?.DashboardUrl);
            return;
        }

        var message = "Bot worker configuration issues detected:"
            + Environment.NewLine
            + string.Join(Environment.NewLine, errors.Select(e => $"  - {e}"));

        if (strict)
        {
            logger.LogCritical("{Message}", message);
            throw new InvalidOperationException(message);
        }

        logger.LogWarning("{Message}", message);
    }

    private static List<string> CollectErrors(IConfiguration configuration, bool strict)
    {
        var errors = new List<string>();

        var discord = configuration.GetSection(BotOptions.SectionName).Get<BotOptions>() ?? new BotOptions();
        CheckSetting(errors, "Discord:Token", discord.Token, strict);

        var api = configuration.GetSection(ApiOptions.SectionName).Get<ApiOptions>() ?? new ApiOptions();
        CheckSetting(errors, "Api:BaseUrl", api.BaseUrl, strict);
        CheckSetting(errors, "Api:ApiKey", api.ApiKey, strict);

        var platform = configuration.GetSection(PlatformOptions.SectionName).Get<PlatformOptions>() ?? new PlatformOptions();
        CheckSetting(errors, "Platform:DashboardUrl", platform.DashboardUrl, strict);

        if (strict)
        {
            if (api.BaseUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("Api:BaseUrl must use HTTPS in Production.");
            }

            if (platform.DashboardUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("Platform:DashboardUrl must use HTTPS in Production.");
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
