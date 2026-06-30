using System.Text.Json;
using DiscordBot.Domain.Constants;

namespace DiscordBot.Domain.Extensions;

public static class PlanModulesExtensions
{
    public static IReadOnlyList<string> ParseAllowedModules(string allowedModulesJson)
    {
        if (string.IsNullOrWhiteSpace(allowedModulesJson))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(allowedModulesJson) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static bool AllowsModule(string allowedModulesJson, string moduleKey)
    {
        var allowed = ParseAllowedModules(allowedModulesJson);
        if (allowed.Contains(PlanKeys.AllModulesToken))
        {
            return true;
        }

        return allowed.Contains(moduleKey);
    }

    public static string SerializeAllowedModules(IEnumerable<string> moduleKeys) =>
        JsonSerializer.Serialize(moduleKeys.ToList());
}
