using System.Text.Json;
using DiscordBot.Domain.Constants;
using DiscordBot.Infrastructure.Models;

namespace DiscordBot.Infrastructure.Services;

internal static class CommandPanelDefaults
{
    public const string Title = "How can we help?";
    public const string Description = "Use the buttons below — no commands needed.";

    public static string DefaultButtonsJson =>
        JsonSerializer.Serialize(DefaultButtons);

    public static IReadOnlyList<CommandPanelButtonDefinition> DefaultButtons =>
    [
        new()
        {
            Id = "ticket-open",
            Action = CommandPanelActions.TicketOpen,
            Label = "Create Ticket",
            Style = "Success",
            Enabled = true,
            Order = 0
        },
        new()
        {
            Id = "ticket-help",
            Action = CommandPanelActions.TicketHelp,
            Label = "Ticket Help",
            Style = "Secondary",
            Enabled = true,
            Order = 1
        }
    ];
}
