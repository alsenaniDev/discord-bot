using Discord;
using Discord.WebSocket;
using DiscordBot.Bot.Api;
using DiscordBot.Bot.Api.Models;
using DiscordBot.Bot.Services;
using DiscordBot.Bot.UI;
using DiscordBot.Domain.Constants;

namespace DiscordBot.Bot.Commands;

public class ReactionRoleCommandHandlers
{
    private readonly BotApiClient _apiClient;
    private readonly EmbedBuilderService _embeds;
    private readonly ComponentBuilderService _components;
    private readonly ModuleGuard _moduleGuard;

    public ReactionRoleCommandHandlers(
        BotApiClient apiClient,
        EmbedBuilderService embeds,
        ComponentBuilderService components,
        ModuleGuard moduleGuard)
    {
        _apiClient = apiClient;
        _embeds = embeds;
        _components = components;
        _moduleGuard = moduleGuard;
    }

    public async Task HandleCreateAsync(SocketInteraction interaction, SocketSlashCommand command)
    {
        var guild = (interaction.User as SocketGuildUser)?.Guild;
        var moderator = interaction.User as SocketGuildUser;

        if (guild is null || moderator is null)
        {
            await InteractionResponseHelper.RespondErrorAsync(
                interaction,
                _embeds,
                "Server only",
                "This command can only be used inside a Discord server.");
            return;
        }

        if (!await _moduleGuard.EnsureEnabledForInteractionAsync(
                interaction,
                guild.Id.ToString(),
                ModuleKeys.ReactionRoles))
        {
            return;
        }

        if (!CanManageReactionRoles(moderator))
        {
            await InteractionResponseHelper.RespondErrorAsync(
                interaction,
                _embeds,
                "Permission denied",
                "You need **Manage Roles** or **Manage Server** to create reaction roles.");
            return;
        }

        var botMember = guild.CurrentUser;
        if (!botMember.GuildPermissions.ManageRoles)
        {
            await InteractionResponseHelper.RespondErrorAsync(
                interaction,
                _embeds,
                "Bot missing permission",
                "I need **Manage Roles** permission to assign roles.");
            return;
        }

        var createOption = command.Data.Options.FirstOrDefault(o => o.Name == "create");
        if (createOption?.Options is null)
        {
            await InteractionResponseHelper.RespondErrorAsync(
                interaction,
                _embeds,
                "Invalid command",
                "Use `/reaction-role create` with all required options.");
            return;
        }

        var channel = createOption.Options.FirstOrDefault(o => o.Name == "channel")?.Value as IGuildChannel;
        var title = createOption.Options.FirstOrDefault(o => o.Name == "title")?.Value?.ToString();
        var description = createOption.Options.FirstOrDefault(o => o.Name == "description")?.Value?.ToString();
        var role = createOption.Options.FirstOrDefault(o => o.Name == "role")?.Value as SocketRole;
        var buttonLabel = createOption.Options.FirstOrDefault(o => o.Name == "button_label")?.Value?.ToString();

        if (channel is not IMessageChannel messageChannel
            || string.IsNullOrWhiteSpace(title)
            || string.IsNullOrWhiteSpace(description)
            || role is null
            || string.IsNullOrWhiteSpace(buttonLabel))
        {
            await InteractionResponseHelper.RespondErrorAsync(
                interaction,
                _embeds,
                "Invalid input",
                "Channel, title, description, role, and button label are required.");
            return;
        }

        if (buttonLabel.Trim().Length > 80)
        {
            await InteractionResponseHelper.RespondErrorAsync(
                interaction,
                _embeds,
                "Button label too long",
                "Button labels must be 80 characters or fewer.");
            return;
        }

        if (!IsAssignableRole(role))
        {
            await InteractionResponseHelper.RespondErrorAsync(
                interaction,
                _embeds,
                "Invalid role",
                "That role is managed by Discord or an integration and cannot be assigned.");
            return;
        }

        if (!CanBotAssignRole(guild, role, out var roleError))
        {
            await InteractionResponseHelper.RespondErrorAsync(
                interaction,
                _embeds,
                "Role hierarchy",
                roleError!);
            return;
        }

        if (messageChannel is IGuildChannel guildChannel)
        {
            var botGuildUser = guild.GetUser(guild.CurrentUser.Id);
            var perms = botGuildUser.GetPermissions(guildChannel);
            if (!perms.SendMessages || !perms.EmbedLinks)
            {
                await InteractionResponseHelper.RespondErrorAsync(
                    interaction,
                    _embeds,
                    "Missing channel access",
                    "I cannot send messages and embeds in that channel.");
                return;
            }
        }

        await interaction.DeferAsync(ephemeral: true);

        var buttonCustomId = DiscordCustomIds.ReactionRoleToggle(Guid.NewGuid());
        var embed = _embeds.BuildReactionRolePanel(title.Trim(), description.Trim(), role);
        var components = _components.BuildReactionRoleButton(buttonCustomId, buttonLabel.Trim());

        IUserMessage postedMessage;
        try
        {
            postedMessage = await messageChannel.SendMessageAsync(embed: embed, components: components);
        }
        catch (Exception)
        {
            await InteractionResponseHelper.FollowupErrorAsync(
                interaction,
                _embeds,
                "Could not post panel",
                "Make sure I can send messages and embeds in that channel.");
            return;
        }

        var saved = await _apiClient.CreateReactionRoleAsync(new CreateReactionRoleApiRequest
        {
            DiscordGuildId = guild.Id.ToString(),
            ChannelDiscordId = messageChannel.Id.ToString(),
            MessageDiscordId = postedMessage.Id.ToString(),
            RoleDiscordId = role.Id.ToString(),
            ButtonCustomId = buttonCustomId,
            Title = title.Trim(),
            Description = description.Trim(),
            ButtonLabel = buttonLabel.Trim(),
            CreatedByDiscordUserId = moderator.Id.ToString()
        });

        if (saved is null)
        {
            try
            {
                await postedMessage.DeleteAsync();
            }
            catch
            {
                // Best effort cleanup.
            }

            await InteractionResponseHelper.FollowupErrorAsync(
                interaction,
                _embeds,
                "Could not save panel",
                "The message was posted but could not be saved to the API. It was removed.");
            return;
        }

        await InteractionResponseHelper.FollowupSuccessAsync(
            interaction,
            _embeds,
            "Reaction role created",
            $"Panel posted in <#{messageChannel.Id}> with role {role.Mention}.");
    }

    private static bool CanManageReactionRoles(SocketGuildUser user) =>
        user.GuildPermissions.ManageRoles || user.GuildPermissions.ManageGuild;

    private static bool IsAssignableRole(SocketRole role) => !role.IsManaged;

    private static bool CanBotAssignRole(SocketGuild guild, SocketRole role, out string? error)
    {
        var botMember = guild.CurrentUser;
        if (role.Position >= botMember.Hierarchy)
        {
            error = "That role is higher than or equal to my highest role. Move my role above it in Server Settings.";
            return false;
        }

        error = null;
        return true;
    }
}
