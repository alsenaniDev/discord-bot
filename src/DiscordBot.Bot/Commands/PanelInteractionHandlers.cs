using Discord;
using Discord.WebSocket;
using DiscordBot.Bot.Configuration;
using DiscordBot.Bot.Services;
using DiscordBot.Bot.UI;
using DiscordBot.Domain.Constants;
using DiscordBot.Bot.Api;
using Microsoft.Extensions.Options;
using DiscordBot.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace DiscordBot.Bot.Commands;

public class PanelInteractionHandlers
{
    private readonly TicketInteractionHandlers _ticketInteractionHandlers;
    private readonly SlashCommandHandlers _slashCommandHandlers;
    private readonly EmbedBuilderService _embeds;
    private readonly PlatformOptions _platformOptions;
    private readonly BotApiClient _apiClient;
    private readonly BotLogWriter _logWriter;
    private readonly ILogger<PanelInteractionHandlers> _logger;

    public PanelInteractionHandlers(
        TicketInteractionHandlers ticketInteractionHandlers,
        SlashCommandHandlers slashCommandHandlers,
        EmbedBuilderService embeds,
        IOptions<PlatformOptions> platformOptions,
        BotApiClient apiClient,
        BotLogWriter logWriter,
        ILogger<PanelInteractionHandlers> logger)
    {
        _ticketInteractionHandlers = ticketInteractionHandlers;
        _slashCommandHandlers = slashCommandHandlers;
        _embeds = embeds;
        _platformOptions = platformOptions.Value;
        _apiClient = apiClient;
        _logWriter = logWriter;
        _logger = logger;
    }

    public async Task HandleButtonAsync(SocketMessageComponent component)
    {
        if (DiscordCustomIds.TryParsePanelButton(component.Data.CustomId, out var panelId, out var buttonId))
        {
            var button = await _apiClient.GetPanelButtonActionAsync(panelId, buttonId);
            var interactionGuildId = (component.User as SocketGuildUser)?.Guild.Id.ToString();
            if (button is null || button.DiscordGuildId != interactionGuildId)
            {
                await InteractionResponseHelper.RespondErrorAsync(component, _embeds, "Invalid panel",
                    "This panel does not belong to this server.");
                return;
            }
            if (button.ActionType == "CreateTicket")
            {
                await _ticketInteractionHandlers.HandleCreateButtonAsync(component);
                return;
            }

            if (button.ActionType == "SendMessage" && !string.IsNullOrWhiteSpace(button.ResponseMessage))
            {
                await component.RespondAsync(button.ResponseMessage, ephemeral: true);
                return;
            }

            if (button.ActionType == "AssignRole")
            {
                await HandleAssignRoleAsync(component, button.RoleDiscordId, panelId);
                return;
            }

            await InteractionResponseHelper.RespondErrorAsync(component, _embeds, "Action unavailable",
                "This panel action is not available yet. Please contact a server administrator.");
            return;
        }

        // TODO: Remove legacy action IDs after all existing panels have been republished.
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

    private async Task HandleAssignRoleAsync(SocketMessageComponent component, string? roleDiscordId, Guid panelId)
    {
        var member = component.User as SocketGuildUser;
        var guild = member?.Guild;
        if (guild is null || member is null || !ulong.TryParse(roleDiscordId, out var roleId))
        {
            await InteractionResponseHelper.RespondErrorAsync(component, _embeds, "Role unavailable",
                "The configured role is invalid or no longer available.");
            return;
        }

        var role = guild.GetRole(roleId);
        var botMember = guild.CurrentUser;
        if (role is null || role.IsManaged)
        {
            await InteractionResponseHelper.RespondErrorAsync(component, _embeds, "Role unavailable",
                "The configured role no longer exists or is managed by Discord.");
            return;
        }
        if (!botMember.GuildPermissions.ManageRoles)
        {
            await InteractionResponseHelper.RespondErrorAsync(component, _embeds, "Bot missing permission",
                "I need **Manage Roles** to update your roles.");
            return;
        }
        if (role.Position >= botMember.Hierarchy)
        {
            await InteractionResponseHelper.RespondErrorAsync(component, _embeds, "Role hierarchy",
                "I cannot manage that role because it is above my highest role.");
            return;
        }

        var hadRole = member.Roles.Any(x => x.Id == role.Id);
        try
        {
            if (hadRole) await member.RemoveRoleAsync(role);
            else await member.AddRoleAsync(role);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Panel {PanelId} failed to toggle role {RoleId} for member {MemberId} in guild {GuildId}.", panelId, role.Id, member.Id, guild.Id);
            await InteractionResponseHelper.RespondErrorAsync(component, _embeds, "Role update failed",
                "Could not update your role. Check the bot permission and role hierarchy.");
            return;
        }

        await _logWriter.WriteAsync(guild.Id.ToString(),
            hadRole ? LogEventType.ReactionRoleRemoved : LogEventType.ReactionRoleAssigned,
            $"{(hadRole ? "Removed" : "Assigned")} role {role.Name} {(hadRole ? "from" : "to")} {member.Username} from panel {panelId}.",
            targetDiscordUserId: member.Id.ToString(), targetDisplayName: member.Username,
            metadataJson: $"{{\"roleId\":\"{role.Id}\",\"panelId\":\"{panelId}\"}}");

        await InteractionResponseHelper.RespondSuccessAsync(component, _embeds,
            hadRole ? "Role removed" : "Role assigned",
            hadRole ? $"The **{role.Name}** role was removed from you." : $"You now have the **{role.Name}** role.");
    }
}
