using DiscordBot.Infrastructure.Models;

namespace DiscordBot.Api.Validation;

public static class AutoReplyValidator
{
    private const int MaxTriggerLength = 500;
    private const int MaxResponseLength = 2000;

    public static IReadOnlyList<string> ValidateCreate(CreateAutoReplyRuleRequest request) =>
        Validate(request.Trigger, request.Response);

    public static IReadOnlyList<string> ValidateUpdate(UpdateAutoReplyRuleRequest request) =>
        Validate(request.Trigger, request.Response);

    private static IReadOnlyList<string> Validate(string trigger, string response)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(trigger))
        {
            errors.Add("Trigger is required.");
        }
        else if (trigger.Trim().Length > MaxTriggerLength)
        {
            errors.Add($"Trigger must be {MaxTriggerLength} characters or less.");
        }

        if (string.IsNullOrWhiteSpace(response))
        {
            errors.Add("Response is required.");
        }
        else if (response.Trim().Length > MaxResponseLength)
        {
            errors.Add($"Response must be {MaxResponseLength} characters or less.");
        }

        return errors;
    }
}
