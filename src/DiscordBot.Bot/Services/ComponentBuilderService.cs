using Discord;
using DiscordBot.Bot.Api.Models;
using DiscordBot.Bot.UI;

namespace DiscordBot.Bot.Services;

/// <summary>
/// Builds reusable Discord message components (buttons, select menus, modals).
/// </summary>
public class ComponentBuilderService
{
    public MessageComponent BuildTicketOpenPromptComponents() =>
        new ComponentBuilder()
            .WithButton(
                label: "Create ticket",
                style: ButtonStyle.Success,
                emote: new Emoji("🎫"),
                customId: DiscordCustomIds.TicketCreate)
            .Build();

    public MessageComponent BuildTicketChannelComponents(ulong channelId) =>
        new ComponentBuilder()
            .WithButton(
                label: "Close ticket",
                style: ButtonStyle.Danger,
                emote: new Emoji("🔒"),
                customId: DiscordCustomIds.TicketCloseButton(channelId))
            .WithSelectMenu(
                BuildTicketActionSelect(channelId),
                row: 1)
            .Build();

    public SelectMenuBuilder BuildTicketActionSelect(ulong channelId) =>
        new SelectMenuBuilder()
            .WithCustomId(DiscordCustomIds.TicketSelectMenu(channelId))
            .WithPlaceholder("Choose a ticket action")
            .WithMinValues(1)
            .WithMaxValues(1)
            .AddOption(
                new SelectMenuOptionBuilder()
                    .WithLabel("Close ticket")
                    .WithDescription("Close this ticket and delete the channel")
                    .WithEmote(new Emoji("🔒"))
                    .WithValue(DiscordCustomIds.TicketSelectClose))
            .AddOption(
                new SelectMenuOptionBuilder()
                    .WithLabel("How does this work?")
                    .WithDescription("Quick guide for using tickets")
                    .WithEmote(new Emoji("❓"))
                    .WithValue(DiscordCustomIds.TicketSelectHelp));

    public MessageComponent BuildReactionRoleButton(string customId, string label) =>
        new ComponentBuilder()
            .WithButton(
                label: label,
                style: ButtonStyle.Primary,
                emote: new Emoji("🎭"),
                customId: customId)
            .Build();

    public MessageComponent BuildCommandPanelComponents(Guid panelId, IEnumerable<CommandPanelButtonApiResponse> buttons)
    {
        var builder = new ComponentBuilder();
        var row = 0;
        var buttonsInRow = 0;

        foreach (var button in buttons.Where(b => b.IsEnabled).OrderBy(b => b.SortOrder))
        {
            if (buttonsInRow == 5)
            {
                row++;
                buttonsInRow = 0;
            }

            var item = new ButtonBuilder().WithLabel(button.Label).WithStyle(MapButtonStyle(button.Style));
            if (!string.IsNullOrWhiteSpace(button.Emoji)) item.WithEmote(Emote.TryParse(button.Emoji, out var emote) ? emote : new Emoji(button.Emoji));
            if (item.Style == ButtonStyle.Link) item.WithUrl(button.Url);
            else item.WithCustomId(DiscordCustomIds.PanelButton(panelId, button.Id));
            builder.WithButton(item, row);
            buttonsInRow++;
        }

        return builder.Build();
    }

    private static ButtonStyle MapButtonStyle(string? style) =>
        style?.Trim() switch
        {
            "Primary" => ButtonStyle.Primary,
            "Success" => ButtonStyle.Success,
            "Danger" => ButtonStyle.Danger,
            "Link" => ButtonStyle.Link,
            _ => ButtonStyle.Secondary
        };

    public Modal BuildCloseTicketModal(ulong channelId) =>
        new ModalBuilder()
            .WithTitle("Close ticket")
            .WithCustomId(DiscordCustomIds.TicketCloseModal(channelId))
            .AddTextInput(
                label: "Confirmation",
                customId: "confirmation",
                placeholder: "Type CLOSE to confirm",
                style: TextInputStyle.Short,
                minLength: 5,
                maxLength: 5,
                required: true)
            .Build();
}
