using Discord;
using Discord.WebSocket;
using DiscordBot.Bot.Configuration;
using DiscordBot.Bot.Services;
using DiscordBot.Bot.UI;
using DiscordBot.Domain.Constants;
using Microsoft.Extensions.Options;

namespace DiscordBot.Bot.Commands;

public class PanelInteractionHandlers
{
    private readonly TicketInteractionHandlers _ticketInteractionHandlers;
    private readonly SlashCommandHandlers _slashCommandHandlers;
    private readonly EmbedBuilderService _embeds;
    private readonly PlatformOptions _platformOptions;

    public PanelInteractionHandlers(
        TicketInteractionHandlers ticketInteractionHandlers,
        SlashCommandHandlers slashCommandHandlers,
        EmbedBuilderService embeds,
        IOptions<PlatformOptions> platformOptions)
    {
        _ticketInteractionHandlers = ticketInteractionHandlers;
        _slashCommandHandlers = slashCommandHandlers;
        _embeds = embeds;
        _platformOptions = platformOptions.Value;
    }

    public async Task HandleButtonAsync(SocketMessageComponent component)
    {
        if (!DiscordCustomIds.TryParsePanelAction(component.Data.CustomId, out var action))
        {
            return;
        }

        switch (action)
        {
            case CommandPanelActions.TicketOpen:
                await _ticketInteractionHandlers.HandleCreateButtonAsync(component);
                break;
            case CommandPanelActions.TicketHelp:
                await component.RespondAsync(embed: _embeds.BuildTicketHelp(), ephemeral: true);
                break;
            case CommandPanelActions.Ping:
                await _slashCommandHandlers.HandlePingAsync(component);
                break;
            case CommandPanelActions.ServerInfo:
                await _slashCommandHandlers.HandleServerAsync(component);
                break;
            case CommandPanelActions.ModerationHelp:
                await component.RespondAsync(embed: _embeds.BuildModerationHelp(), ephemeral: true);
                break;
            case CommandPanelActions.ReactionRolesHelp:
                await component.RespondAsync(embed: _embeds.BuildReactionRolesHelp(), ephemeral: true);
                break;
            case CommandPanelActions.PlatformHelp:
                await component.RespondAsync(
                    embed: _embeds.BuildPlatformHelp(_platformOptions.DashboardUrl),
                    ephemeral: true);
                break;
        }
    }
}
