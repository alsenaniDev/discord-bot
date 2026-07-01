using Discord;
using Discord.WebSocket;
using DiscordBot.Bot.Api;
using DiscordBot.Bot.Api.Models;
using DiscordBot.Bot.Services;
using DiscordBot.Bot.UI;
using DiscordBot.Domain.Constants;

namespace DiscordBot.Bot.Commands;

public class ModerationCommandHandlers
{
    private readonly BotApiClient _apiClient;
    private readonly EmbedBuilderService _embeds;
    private readonly ModuleGuard _moduleGuard;

    public ModerationCommandHandlers(
        BotApiClient apiClient,
        EmbedBuilderService embeds,
        ModuleGuard moduleGuard)
    {
        _apiClient = apiClient;
        _embeds = embeds;
        _moduleGuard = moduleGuard;
    }

    public async Task HandleWarnAsync(SocketInteraction interaction, SocketSlashCommand command)
    {
        var context = await TryGetModerationContextAsync(interaction);
        if (context is null)
        {
            return;
        }

        var (guild, moderator) = context.Value;
        if (!await EnsurePermissionAsync(interaction, guild, moderator, p => p.CanWarn, "warn members"))
        {
            return;
        }

        var target = command.Data.Options.FirstOrDefault(o => o.Name == "user")?.Value as SocketGuildUser;
        var reason = command.Data.Options.FirstOrDefault(o => o.Name == "reason")?.Value?.ToString();

        if (target is null || string.IsNullOrWhiteSpace(reason))
        {
            await InteractionResponseHelper.RespondErrorAsync(
                interaction,
                _embeds,
                "Invalid input",
                "A user and reason are required.");
            return;
        }

        if (target.Id == moderator.Id)
        {
            await InteractionResponseHelper.RespondErrorAsync(
                interaction,
                _embeds,
                "Invalid target",
                "You cannot warn yourself.");
            return;
        }

        await interaction.DeferAsync(ephemeral: true);

        var warning = await _apiClient.CreateWarningAsync(new CreateWarningApiRequest
        {
            DiscordGuildId = guild.Id.ToString(),
            TargetDiscordUserId = target.Id.ToString(),
            ModeratorDiscordUserId = moderator.Id.ToString(),
            Reason = reason.Trim(),
            ModeratorDisplayName = moderator.GlobalName ?? moderator.DisplayName,
            TargetDisplayName = target.GlobalName ?? target.DisplayName
        });

        if (warning is null)
        {
            await InteractionResponseHelper.FollowupErrorAsync(
                interaction,
                _embeds,
                "Warning failed",
                "Could not save the warning. Make sure the API is running and the server is registered.");
            return;
        }

        await InteractionResponseHelper.FollowupSuccessAsync(
            interaction,
            _embeds,
            "Member warned",
            $"{target.Mention} was warned.\n**Reason:** {reason.Trim()}");
    }

    public async Task HandleWarningsAsync(SocketInteraction interaction, SocketSlashCommand command)
    {
        var context = await TryGetModerationContextAsync(interaction);
        if (context is null)
        {
            return;
        }

        var (guild, _) = context.Value;
        var moderator = interaction.User as SocketGuildUser;
        if (moderator is null)
        {
            return;
        }

        if (!await EnsurePermissionAsync(interaction, guild, moderator, p => p.CanWarn || p.CanAccessModeration, "view warnings"))
        {
            return;
        }

        var target = command.Data.Options.FirstOrDefault(o => o.Name == "user")?.Value as SocketGuildUser;
        if (target is null)
        {
            await InteractionResponseHelper.RespondErrorAsync(
                interaction,
                _embeds,
                "Invalid input",
                "A user is required.");
            return;
        }

        await interaction.DeferAsync(ephemeral: true);

        var warnings = await _apiClient.GetWarningsAsync(guild.Id.ToString(), target.Id.ToString());
        if (warnings.Count == 0)
        {
            await InteractionResponseHelper.FollowupSuccessAsync(
                interaction,
                _embeds,
                "No warnings",
                $"{target.Mention} has no recorded warnings.");
            return;
        }

        var lines = warnings
            .Take(10)
            .Select((w, index) =>
                $"**{index + 1}.** {w.Reason} — <t:{w.CreatedAt.ToUnixTimeSeconds()}:R>")
            .ToList();

        var description = string.Join("\n", lines);
        if (warnings.Count > 10)
        {
            description += $"\n\n*Showing 10 of {warnings.Count} warnings.*";
        }

        await interaction.FollowupAsync(
            embed: _embeds.BuildInfo(
                $"Warnings for {target.Username}",
                description),
            ephemeral: true);
    }

    public async Task HandleClearAsync(SocketInteraction interaction, SocketSlashCommand command)
    {
        var context = await TryGetModerationContextAsync(interaction);
        if (context is null)
        {
            return;
        }

        var (guild, moderator) = context.Value;

        if (!await EnsurePermissionAsync(interaction, guild, moderator, p => p.CanClearMessages, "clear messages"))
        {
            return;
        }

        if (interaction.Channel is not SocketTextChannel textChannel)
        {
            await InteractionResponseHelper.RespondErrorAsync(
                interaction,
                _embeds,
                "Text channel only",
                "This command can only be used in a text channel.");
            return;
        }

        var amountOption = command.Data.Options.FirstOrDefault(o => o.Name == "amount")?.Value;
        if (amountOption is not long amountLong || amountLong < 1 || amountLong > 100)
        {
            await InteractionResponseHelper.RespondErrorAsync(
                interaction,
                _embeds,
                "Invalid amount",
                "Amount must be between 1 and 100.");
            return;
        }

        var amount = (int)amountLong;

        await interaction.DeferAsync(ephemeral: true);

        IReadOnlyList<IMessage> messages;
        try
        {
            messages = (await textChannel.GetMessagesAsync(amount).FlattenAsync()).ToList();
        }
        catch (Exception)
        {
            await InteractionResponseHelper.FollowupErrorAsync(
                interaction,
                _embeds,
                "Could not read messages",
                "Make sure the bot has **Read Message History** permission.");
            return;
        }

        var deletable = messages.Where(m => m.CreatedAt > DateTimeOffset.UtcNow.AddDays(-14)).ToList();
        if (deletable.Count == 0)
        {
            await InteractionResponseHelper.FollowupErrorAsync(
                interaction,
                _embeds,
                "Nothing to delete",
                "No messages found, or all messages are older than 14 days.");
            return;
        }

        try
        {
            await textChannel.DeleteMessagesAsync(deletable);
        }
        catch (Exception)
        {
            await InteractionResponseHelper.FollowupErrorAsync(
                interaction,
                _embeds,
                "Delete failed",
                "Make sure the bot has **Manage Messages** permission.");
            return;
        }

        var saved = await _apiClient.CreateModerationCaseAsync(new CreateModerationCaseApiRequest
        {
            DiscordGuildId = guild.Id.ToString(),
            Type = 2,
            ModeratorDiscordUserId = moderator.Id.ToString(),
            MessageCount = deletable.Count,
            ChannelDiscordId = textChannel.Id.ToString(),
            Reason = $"Cleared {deletable.Count} message(s)",
            ModeratorDisplayName = moderator.GlobalName ?? moderator.DisplayName,
            ChannelDisplayName = textChannel.Name
        });

        if (!saved)
        {
            await InteractionResponseHelper.FollowupErrorAsync(
                interaction,
                _embeds,
                "Messages deleted",
                $"Deleted {deletable.Count} message(s), but the case could not be saved to the API.");
            return;
        }

        await InteractionResponseHelper.FollowupSuccessAsync(
            interaction,
            _embeds,
            "Messages cleared",
            $"Deleted **{deletable.Count}** message(s) in {textChannel.Mention}.");
    }

    public async Task HandleKickAsync(SocketInteraction interaction, SocketSlashCommand command)
    {
        var context = await TryGetModerationContextAsync(interaction);
        if (context is null)
        {
            return;
        }

        var (guild, moderator) = context.Value;

        if (!await EnsurePermissionAsync(interaction, guild, moderator, p => p.CanKick, "kick members"))
        {
            return;
        }

        if (!moderator.GuildPermissions.KickMembers)
        {
            await InteractionResponseHelper.RespondErrorAsync(
                interaction,
                _embeds,
                "Permission denied",
                "You need **Kick Members** to use this command.");
            return;
        }

        var botMember = guild.CurrentUser;
        if (!botMember.GuildPermissions.KickMembers)
        {
            await InteractionResponseHelper.RespondErrorAsync(
                interaction,
                _embeds,
                "Bot missing permission",
                "I need **Kick Members** permission to kick members.");
            return;
        }

        var target = command.Data.Options.FirstOrDefault(o => o.Name == "user")?.Value as SocketGuildUser;
        var reason = command.Data.Options.FirstOrDefault(o => o.Name == "reason")?.Value?.ToString();

        if (target is null || string.IsNullOrWhiteSpace(reason))
        {
            await InteractionResponseHelper.RespondErrorAsync(
                interaction,
                _embeds,
                "Invalid input",
                "A user and reason are required.");
            return;
        }

        if (target.Id == moderator.Id || target.Id == botMember.Id)
        {
            await InteractionResponseHelper.RespondErrorAsync(
                interaction,
                _embeds,
                "Invalid target",
                "That member cannot be kicked.");
            return;
        }

        if (target.Hierarchy >= moderator.Hierarchy || target.Hierarchy >= botMember.Hierarchy)
        {
            await InteractionResponseHelper.RespondErrorAsync(
                interaction,
                _embeds,
                "Hierarchy error",
                "You cannot kick a member with an equal or higher role.");
            return;
        }

        await interaction.DeferAsync(ephemeral: true);

        try
        {
            await target.KickAsync(reason.Trim());
        }
        catch (Exception)
        {
            await InteractionResponseHelper.FollowupErrorAsync(
                interaction,
                _embeds,
                "Kick failed",
                "Could not kick the member. Check permissions and role hierarchy.");
            return;
        }

        var saved = await _apiClient.CreateModerationCaseAsync(new CreateModerationCaseApiRequest
        {
            DiscordGuildId = guild.Id.ToString(),
            Type = 1,
            TargetDiscordUserId = target.Id.ToString(),
            ModeratorDiscordUserId = moderator.Id.ToString(),
            Reason = reason.Trim(),
            ModeratorDisplayName = moderator.GlobalName ?? moderator.DisplayName,
            TargetDisplayName = target.GlobalName ?? target.DisplayName
        });

        if (!saved)
        {
            await InteractionResponseHelper.FollowupErrorAsync(
                interaction,
                _embeds,
                "Member kicked",
                $"{target.Username} was kicked, but the case could not be saved to the API.");
            return;
        }

        await InteractionResponseHelper.FollowupSuccessAsync(
            interaction,
            _embeds,
            "Member kicked",
            $"{target.Username} was kicked.\n**Reason:** {reason.Trim()}");
    }

    private async Task<(SocketGuild Guild, SocketGuildUser Moderator)?> TryGetModerationContextAsync(
        SocketInteraction interaction)
    {
        if (!interaction.GuildId.HasValue)
        {
            await InteractionResponseHelper.RespondErrorAsync(
                interaction,
                _embeds,
                "Server only",
                "This command can only be used inside a Discord server.");
            return null;
        }

        var guild = (interaction.User as SocketGuildUser)?.Guild;
        var moderator = interaction.User as SocketGuildUser;

        if (guild is null || moderator is null)
        {
            await InteractionResponseHelper.RespondErrorAsync(
                interaction,
                _embeds,
                "Server only",
                "This command can only be used inside a Discord server.");
            return null;
        }

        if (!await _moduleGuard.EnsureEnabledForInteractionAsync(
                interaction,
                guild.Id.ToString(),
                ModuleKeys.Moderation))
        {
            return null;
        }

        if (!await HasModerationAccessAsync(guild, moderator))
        {
            await InteractionResponseHelper.RespondErrorAsync(
                interaction,
                _embeds,
                "Permission denied",
                "Your Discord role is not mapped to a moderation permission role. Ask the server owner to configure **Staff → Roles & permissions** in the dashboard.");
            return null;
        }

        return (guild, moderator);
    }

    private async Task<bool> HasModerationAccessAsync(SocketGuild guild, SocketGuildUser user)
    {
        if (guild.OwnerId == user.Id)
        {
            return true;
        }

        var permissions = await GetPermissionsAsync(guild, user);
        return permissions?.CanAccessModeration == true;
    }

    private Task<EvaluatePermissionsApiResponse?> GetPermissionsAsync(SocketGuild guild, SocketGuildUser user)
    {
        var roleIds = user.Roles.Select(r => r.Id.ToString()).ToList();
        return _apiClient.EvaluatePermissionsAsync(
            guild.Id.ToString(),
            user.Id.ToString(),
            roleIds);
    }

    private async Task<bool> EnsurePermissionAsync(
        SocketInteraction interaction,
        SocketGuild guild,
        SocketGuildUser user,
        Func<EvaluatePermissionsApiResponse, bool> predicate,
        string actionLabel)
    {
        if (guild.OwnerId == user.Id)
        {
            return true;
        }

        var permissions = await GetPermissionsAsync(guild, user);
        if (permissions is not null && predicate(permissions))
        {
            return true;
        }

        await InteractionResponseHelper.RespondErrorAsync(
            interaction,
            _embeds,
            "Permission denied",
            $"Your role is not allowed to {actionLabel}. Configure permissions under **Staff → Roles & permissions** in the dashboard.");
        return false;
    }
}
