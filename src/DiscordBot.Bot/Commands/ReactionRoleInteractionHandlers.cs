using Discord;
using Discord.WebSocket;
using DiscordBot.Bot.Api;
using DiscordBot.Bot.Api.Models;
using DiscordBot.Bot.Services;
using DiscordBot.Bot.UI;
using DiscordBot.Domain.Constants;
using DiscordBot.Domain.Enums;

namespace DiscordBot.Bot.Commands;

public class ReactionRoleInteractionHandlers
{
    private readonly BotApiClient _apiClient;
    private readonly EmbedBuilderService _embeds;
    private readonly ModuleGuard _moduleGuard;
    private readonly BotLogWriter _logWriter;

    public ReactionRoleInteractionHandlers(
        BotApiClient apiClient,
        EmbedBuilderService embeds,
        ModuleGuard moduleGuard,
        BotLogWriter logWriter)
    {
        _apiClient = apiClient;
        _embeds = embeds;
        _moduleGuard = moduleGuard;
        _logWriter = logWriter;
    }

    public async Task HandleButtonAsync(SocketMessageComponent component)
    {
        var guild = (component.User as SocketGuildUser)?.Guild;
        var member = component.User as SocketGuildUser;

        if (guild is null || member is null)
        {
            await InteractionResponseHelper.RespondErrorAsync(
                component,
                _embeds,
                "Unavailable",
                "This action can only be used inside a server.");
            return;
        }

        if (!await _moduleGuard.EnsureEnabledForInteractionAsync(
                component,
                guild.Id.ToString(),
                ModuleKeys.ReactionRoles))
        {
            return;
        }

        var panel = await _apiClient.GetReactionRoleByButtonAsync(component.Data.CustomId);
        if (panel is null)
        {
            await InteractionResponseHelper.RespondErrorAsync(
                component,
                _embeds,
                "Unknown panel",
                "This reaction role panel is no longer registered.");
            return;
        }

        if (!panel.IsActive)
        {
            await InteractionResponseHelper.RespondInfoAsync(
                component,
                _embeds,
                "Panel inactive",
                "This reaction role panel has been deactivated.");
            return;
        }

        var role = guild.GetRole(ulong.Parse(panel.RoleDiscordId));
        if (role is null)
        {
            await InteractionResponseHelper.RespondErrorAsync(
                component,
                _embeds,
                "Role missing",
                "The configured role no longer exists in this server.");
            return;
        }

        if (role.IsManaged)
        {
            await InteractionResponseHelper.RespondErrorAsync(
                component,
                _embeds,
                "Role unavailable",
                "That role is managed and cannot be assigned.");
            return;
        }

        var botMember = guild.CurrentUser;
        if (!botMember.GuildPermissions.ManageRoles)
        {
            await InteractionResponseHelper.RespondErrorAsync(
                component,
                _embeds,
                "Bot missing permission",
                "I need **Manage Roles** to update your roles.");
            return;
        }

        if (role.Position >= botMember.Hierarchy)
        {
            await InteractionResponseHelper.RespondErrorAsync(
                component,
                _embeds,
                "Role hierarchy",
                "I cannot assign that role because it is above my highest role.");
            return;
        }

        var hasRole = member.Roles.Any(r => r.Id == role.Id);

        try
        {
            if (hasRole)
            {
                await member.RemoveRoleAsync(role);
            }
            else
            {
                await member.AddRoleAsync(role);
            }
        }
        catch (Exception)
        {
            await InteractionResponseHelper.RespondErrorAsync(
                component,
                _embeds,
                "Role update failed",
                "Could not update your role. Check permissions and role hierarchy.");
            return;
        }

        var guildId = guild.Id.ToString();
        if (hasRole)
        {
            await _logWriter.WriteAsync(
                guildId,
                LogEventType.ReactionRoleRemoved,
                $"Removed role {role.Name} from {member.Username}.",
                targetDiscordUserId: member.Id.ToString(),
                metadataJson: $"{{\"roleId\":\"{role.Id}\",\"panelId\":\"{panel.Id}\"}}");

            await InteractionResponseHelper.RespondSuccessAsync(
                component,
                _embeds,
                "Role removed",
                $"The **{role.Name}** role was removed from you.");
        }
        else
        {
            await _logWriter.WriteAsync(
                guildId,
                LogEventType.ReactionRoleAssigned,
                $"Assigned role {role.Name} to {member.Username}.",
                targetDiscordUserId: member.Id.ToString(),
                metadataJson: $"{{\"roleId\":\"{role.Id}\",\"panelId\":\"{panel.Id}\"}}");

            await InteractionResponseHelper.RespondSuccessAsync(
                component,
                _embeds,
                "Role assigned",
                $"You now have the **{role.Name}** role.");
        }
    }
}
