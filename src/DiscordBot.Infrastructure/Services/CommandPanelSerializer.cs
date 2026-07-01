using System.Text.Json;
using DiscordBot.Domain.Constants;
using DiscordBot.Infrastructure.Models;

namespace DiscordBot.Infrastructure.Services;

internal static class CommandPanelSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static IReadOnlyList<CommandPanelButtonDefinition> ParseButtons(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return CommandPanelDefaults.DefaultButtons;
        }

        try
        {
            var buttons = JsonSerializer.Deserialize<List<CommandPanelButtonDefinition>>(json, JsonOptions);
            return NormalizeButtons(buttons);
        }
        catch (JsonException)
        {
            return CommandPanelDefaults.DefaultButtons;
        }
    }

    public static string SerializeButtons(IEnumerable<CommandPanelButtonDefinition> buttons) =>
        JsonSerializer.Serialize(NormalizeButtons(buttons));

    public static IReadOnlyList<CommandPanelButtonDefinition> NormalizeButtons(
        IEnumerable<CommandPanelButtonDefinition>? buttons)
    {
        if (buttons is null)
        {
            return CommandPanelDefaults.DefaultButtons;
        }

        return buttons
            .Where(b => !string.IsNullOrWhiteSpace(b.Action) && CommandPanelActions.All.Contains(b.Action))
            .Where(b => !string.IsNullOrWhiteSpace(b.Label))
            .Select((b, index) => new CommandPanelButtonDefinition
            {
                Id = string.IsNullOrWhiteSpace(b.Id) ? $"btn-{index}" : b.Id.Trim(),
                Action = b.Action.Trim(),
                Label = b.Label.Trim()[..Math.Min(b.Label.Trim().Length, 80)],
                Style = NormalizeStyle(b.Style),
                Enabled = b.Enabled,
                Order = b.Order
            })
            .OrderBy(b => b.Order)
            .ThenBy(b => b.Label, StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .ToList();
    }

    private static string NormalizeStyle(string? style) =>
        style?.Trim() switch
        {
            "Primary" => "Primary",
            "Success" => "Success",
            "Danger" => "Danger",
            _ => "Secondary"
        };
}
