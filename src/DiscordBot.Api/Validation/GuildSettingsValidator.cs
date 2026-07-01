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

        ValidateTicketTemplateField(errors, "Ticket welcome title", request.TicketWelcomeTitle, 256, required: true);
        ValidateTicketTemplateField(errors, "Ticket welcome message", request.TicketWelcomeMessage, MaxMessageLength, required: true);
        ValidateTicketTemplateField(errors, "Ticket closed message", request.TicketClosedMessage, MaxMessageLength, required: true);
        ValidateTicketTemplateField(errors, "Ticket closed from dashboard message", request.TicketClosedFromDashboardMessage, MaxMessageLength, required: true);
        ValidateTicketTemplateField(errors, "Ticket staff reply prefix", request.TicketStaffReplyPrefix, MaxMessageLength, required: true);

        if (request.CommandPanelEnabled && !IsValidSnowflake(request.CommandPanelChannelId))
        {
            errors.Add("Command panel channel is required when the member panel is enabled.");
        }

        if (string.IsNullOrWhiteSpace(request.CommandPanelTitle))
        {
            errors.Add("Command panel title is required.");
        }
        else if (request.CommandPanelTitle.Length > 256)
        {
            errors.Add("Command panel title must be 256 characters or less.");
        }

        if (string.IsNullOrWhiteSpace(request.CommandPanelDescription))
        {
            errors.Add("Command panel description is required.");
        }
        else if (request.CommandPanelDescription.Length > 2000)
        {
            errors.Add("Command panel description must be 2000 characters or less.");
        }

        if (request.CommandPanelEnabled)
        {
            var enabledButtons = request.CommandPanelButtons.Count(b => b.Enabled);
            if (enabledButtons == 0)
            {
                errors.Add("Enable at least one panel button when the member panel is enabled.");
            }
        }

        return errors;
    }

    private static void ValidateTicketTemplateField(
        List<string> errors,
        string label,
        string? value,
        int maxLength,
        bool required)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            if (required)
            {
                errors.Add($"{label} is required.");
            }

            return;
        }

        if (value.Length > maxLength)
        {
            errors.Add($"{label} must be {maxLength} characters or less.");
        }
    }

    private static bool IsValidSnowflake(string? value) =>
        !string.IsNullOrWhiteSpace(value) && SnowflakeRegex().IsMatch(value);

    [GeneratedRegex(@"^\d{17,20}$")]
    private static partial Regex SnowflakeRegex();
}
