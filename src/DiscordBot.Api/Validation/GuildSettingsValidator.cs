using System.Text.RegularExpressions;
using DiscordBot.Infrastructure.Models;

namespace DiscordBot.Api.Validation;

public static partial class GuildSettingsValidator
{
    private const int MaxMessageLength = 2000;

    public static IReadOnlyList<string> Validate(UpdateGuildSettingsRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.WelcomeMessage))
        {
            errors.Add("Welcome message is required.");
        }
        else if (request.WelcomeMessage.Length > MaxMessageLength)
        {
            errors.Add($"Welcome message must be {MaxMessageLength} characters or less.");
        }

        if (request.WelcomeEnabled && !IsValidSnowflake(request.WelcomeChannelId))
        {
            errors.Add("Welcome channel ID is required and must be a valid Discord snowflake when welcome is enabled.");
        }

        if (request.AutoRoleEnabled && !IsValidSnowflake(request.AutoRoleId))
        {
            errors.Add("Auto role ID is required and must be a valid Discord snowflake when auto role is enabled.");
        }

        if (request.LogsEnabled && !IsValidSnowflake(request.LogChannelId))
        {
            errors.Add("Log channel ID is required and must be a valid Discord snowflake when logs are enabled.");
        }

        if (!string.IsNullOrWhiteSpace(request.WelcomeChannelId) && !IsValidSnowflake(request.WelcomeChannelId))
        {
            errors.Add("Welcome channel ID must be a numeric Discord snowflake.");
        }

        if (!string.IsNullOrWhiteSpace(request.AutoRoleId) && !IsValidSnowflake(request.AutoRoleId))
        {
            errors.Add("Auto role ID must be a numeric Discord snowflake.");
        }

        if (!string.IsNullOrWhiteSpace(request.LogChannelId) && !IsValidSnowflake(request.LogChannelId))
        {
            errors.Add("Log channel ID must be a numeric Discord snowflake.");
        }

        if (!string.IsNullOrWhiteSpace(request.TicketCategoryId) && !IsValidSnowflake(request.TicketCategoryId))
        {
            errors.Add("Ticket category ID must be a numeric Discord snowflake.");
        }

        return errors;
    }

    private static bool IsValidSnowflake(string? value) =>
        !string.IsNullOrWhiteSpace(value) && SnowflakeRegex().IsMatch(value);

    [GeneratedRegex(@"^\d{17,20}$")]
    private static partial Regex SnowflakeRegex();
}
