using Discord;
using Discord.WebSocket;
using DiscordBot.Bot.Api;
using DiscordBot.Bot.Api.Models;
using DiscordBot.Bot.Services;
using DiscordBot.Bot.UI;
using DiscordBot.Domain.Constants;

namespace DiscordBot.Bot.Commands;

public class TicketCommandHandlers
{
    private readonly DiscordSocketClient _client;
    private readonly BotApiClient _apiClient;
    private readonly EmbedBuilderService _embeds;
    private readonly ComponentBuilderService _components;
    private readonly ResourceSyncService _resourceSyncService;
    private readonly ModuleGuard _moduleGuard;

    public TicketCommandHandlers(
        DiscordSocketClient client,
        BotApiClient apiClient,
        EmbedBuilderService embeds,
        ComponentBuilderService components,
        ResourceSyncService resourceSyncService,
        ModuleGuard moduleGuard)
    {
        _client = client;
        _apiClient = apiClient;
        _embeds = embeds;
        _components = components;
        _resourceSyncService = resourceSyncService;
        _moduleGuard = moduleGuard;
    }

    public async Task HandleSetupAsync(SocketInteraction interaction)
    {
        var guild = GetGuild(interaction);
        if (guild is null)
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
                ModuleKeys.Tickets))
        {
            return;
        }

        var user = interaction.User as SocketGuildUser;
        if (user is null || !user.GuildPermissions.ManageGuild)
        {
            await InteractionResponseHelper.RespondErrorAsync(
                interaction,
                _embeds,
                "Permission denied",
                "You need the **Manage Server** permission to set up tickets.");
            return;
        }

        await interaction.DeferAsync(ephemeral: true);

        ICategoryChannel category;
        try
        {
            category = await guild.CreateCategoryChannelAsync("Tickets");
        }
        catch (Exception)
        {
            await InteractionResponseHelper.FollowupErrorAsync(
                interaction,
                _embeds,
                "Could not create category",
                "Make sure the bot has **Manage Channels** permission and try again.");
            return;
        }

        var success = await _apiClient.SetupTicketsAsync(
            guild.Id.ToString(),
            category.Id.ToString());

        if (!success)
        {
            await category.DeleteAsync();
            await InteractionResponseHelper.FollowupErrorAsync(
                interaction,
                _embeds,
                "API registration required",
                "Could not enable tickets in the API. Run `/setup` first and ensure the API is running.");
            return;
        }

        await interaction.FollowupAsync(
            embed: _embeds.BuildTicketSetupSuccess(category, guild),
            ephemeral: true);

        await _resourceSyncService.SyncGuildAsync(guild);
    }

    public async Task HandleOpenAsync(SocketInteraction interaction)
    {
        var guild = GetGuild(interaction);
        if (guild is null)
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
                ModuleKeys.Tickets))
        {
            return;
        }

        var user = interaction.User as SocketGuildUser;
        if (user is null)
        {
            await InteractionResponseHelper.RespondErrorAsync(
                interaction,
                _embeds,
                "Member not found",
                "Could not resolve your membership in this server.");
            return;
        }

        var validationError = await ValidateTicketOpenAsync(guild);
        if (validationError is not null)
        {
            await InteractionResponseHelper.RespondErrorAsync(
                interaction,
                _embeds,
                validationError.Value.Title,
                validationError.Value.Description);
            return;
        }

        await interaction.RespondAsync(
            embed: _embeds.BuildTicketOpenPrompt(),
            components: _components.BuildTicketOpenPromptComponents(),
            ephemeral: true);
    }

    public async Task HandleCloseAsync(SocketInteraction interaction)
    {
        if (interaction.Channel is not ITextChannel channel)
        {
            await InteractionResponseHelper.RespondErrorAsync(
                interaction,
                _embeds,
                "Wrong channel",
                "Run this command inside your ticket channel, or use the **Close ticket** button there.");
            return;
        }

        var guild = GetGuild(interaction);
        if (guild is null)
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
                ModuleKeys.Tickets))
        {
            return;
        }

        var user = interaction.User as SocketGuildUser;
        if (user is null)
        {
            await InteractionResponseHelper.RespondErrorAsync(
                interaction,
                _embeds,
                "Member not found",
                "Could not resolve your membership in this server.");
            return;
        }

        var accessError = await ValidateTicketCloseAccessAsync(channel, user);
        if (accessError is not null)
        {
            await InteractionResponseHelper.RespondErrorAsync(
                interaction,
                _embeds,
                accessError.Value.Title,
                accessError.Value.Description);
            return;
        }

        await interaction.RespondWithModalAsync(_components.BuildCloseTicketModal(channel.Id));
    }

    public async Task<TicketCreateResult> CreateTicketAsync(SocketGuild guild, SocketGuildUser user)
    {
        var validationError = await ValidateTicketOpenAsync(guild);
        if (validationError is not null)
        {
            return new TicketCreateResult(
                false,
                ErrorTitle: validationError.Value.Title,
                ErrorDescription: validationError.Value.Description);
        }

        var settings = await _apiClient.GetSettingsAsync(guild.Id.ToString());
        var categoryId = ulong.Parse(settings!.TicketCategoryId!);

        var overwrites = BuildTicketOverwrites(guild, user);
        ITextChannel channel;

        try
        {
            channel = await guild.CreateTextChannelAsync(
                $"ticket-{user.Username}".ToLowerInvariant(),
                props =>
                {
                    props.CategoryId = categoryId;
                    props.PermissionOverwrites = overwrites;
                    props.Topic = $"Support ticket for {user.Username}";
                });
        }
        catch (Exception)
        {
            return new TicketCreateResult(
                false,
                ErrorTitle: "Could not create channel",
                ErrorDescription: "The bot may be missing **Manage Channels** permission.");
        }

        var (ticket, errorMessage) = await _apiClient.CreateTicketAsync(new CreateTicketApiRequest
        {
            DiscordGuildId = guild.Id.ToString(),
            OwnerDiscordUserId = user.Id.ToString(),
            ChannelDiscordId = channel.Id.ToString(),
            OwnerDisplayName = user.GlobalName ?? user.DisplayName,
            ChannelDisplayName = channel.Name
        });

        if (ticket is null)
        {
            await channel.DeleteAsync();
            return new TicketCreateResult(
                false,
                ErrorTitle: "Could not create ticket",
                ErrorDescription: errorMessage ?? "The ticket could not be saved. Please try again.");
        }

        await channel.ModifyAsync(props => props.Name = $"ticket-{ticket.TicketNumber}");

        await channel.SendMessageAsync(
            embed: _embeds.BuildTicketWelcome(user, ticket.TicketNumber, settings!),
            components: _components.BuildTicketChannelComponents(channel.Id));

        return new TicketCreateResult(true, channel, ticket.TicketNumber);
    }

    public async Task<TicketCloseResult> CloseTicketAsync(
        SocketGuild guild,
        ITextChannel channel,
        SocketGuildUser user)
    {
        var accessError = await ValidateTicketCloseAccessAsync(channel, user);
        if (accessError is not null)
        {
            return new TicketCloseResult(
                false,
                ErrorTitle: accessError.Value.Title,
                ErrorDescription: accessError.Value.Description);
        }

        var ticket = await _apiClient.GetTicketByChannelAsync(channel.Id.ToString());
        if (ticket is null)
        {
            return new TicketCloseResult(
                false,
                ErrorTitle: "Not a ticket channel",
                ErrorDescription: "This channel is not linked to an open support ticket.");
        }

        var closed = await _apiClient.CloseTicketAsync(ticket.Id, new CloseTicketApiRequest
        {
            ClosedByDiscordUserId = user.Id.ToString(),
            ClosedByDisplayName = user.Username,
            ChannelDisplayName = channel.Name
        });
        if (closed is null)
        {
            return new TicketCloseResult(
                false,
                ErrorTitle: "Already closed",
                ErrorDescription: "This ticket is already closed or could not be updated.");
        }

        var settings = await _apiClient.GetSettingsAsync(guild.Id.ToString());
        await channel.SendMessageAsync(
            embed: _embeds.BuildTicketClosed(
                ticket.TicketNumber,
                user,
                settings ?? new GuildSettingsResponse()));
        await Task.Delay(TimeSpan.FromSeconds(3));
        await channel.DeleteAsync();

        return new TicketCloseResult(true, ticket.TicketNumber);
    }

    private async Task<(string Title, string Description)?> ValidateTicketOpenAsync(SocketGuild guild)
    {
        var settings = await _apiClient.GetSettingsAsync(guild.Id.ToString());
        if (settings is null || !settings.TicketsEnabled || string.IsNullOrWhiteSpace(settings.TicketCategoryId))
        {
            return (
                "Tickets not enabled",
                "An administrator must run `/ticket setup` before members can open tickets.");
        }

        if (!ulong.TryParse(settings.TicketCategoryId, out _))
        {
            return (
                "Misconfigured tickets",
                "The ticket category is invalid. Ask an admin to run `/ticket setup` again.");
        }

        return null;
    }

    public async Task<(string Title, string Description)?> ValidateCloseAccessAsync(
        ITextChannel channel,
        SocketGuildUser user) =>
        await ValidateTicketCloseAccessAsync(channel, user);

    private async Task<(string Title, string Description)?> ValidateTicketCloseAccessAsync(
        ITextChannel channel,
        SocketGuildUser user)
    {
        var ticket = await _apiClient.GetTicketByChannelAsync(channel.Id.ToString());
        if (ticket is null)
        {
            return (
                "Not a ticket channel",
                "This channel is not linked to a support ticket.");
        }

        var isOwner = ticket.OwnerDiscordUserId == user.Id.ToString();
        var isStaff = user.GuildPermissions.ManageGuild || user.GuildPermissions.Administrator;

        if (!isOwner && !isStaff)
        {
            return (
                "Permission denied",
                "Only the ticket owner or server staff can close this ticket.");
        }

        return null;
    }

    private static List<Overwrite> BuildTicketOverwrites(SocketGuild guild, SocketGuildUser owner)
    {
        var overwrites = new List<Overwrite>
        {
            new(
                guild.EveryoneRole.Id,
                PermissionTarget.Role,
                new OverwritePermissions(viewChannel: PermValue.Deny)),
            new(
                owner.Id,
                PermissionTarget.User,
                new OverwritePermissions(
                    viewChannel: PermValue.Allow,
                    sendMessages: PermValue.Allow,
                    readMessageHistory: PermValue.Allow,
                    attachFiles: PermValue.Allow))
        };

        foreach (var role in guild.Roles.Where(r =>
                     r.Id != guild.EveryoneRole.Id
                     && (r.Permissions.Administrator || r.Permissions.ManageGuild)))
        {
            overwrites.Add(new Overwrite(
                role.Id,
                PermissionTarget.Role,
                new OverwritePermissions(
                    viewChannel: PermValue.Allow,
                    sendMessages: PermValue.Allow,
                    readMessageHistory: PermValue.Allow,
                    manageMessages: PermValue.Allow)));
        }

        return overwrites;
    }

    private SocketGuild? GetGuild(SocketInteraction interaction)
    {
        if (!interaction.GuildId.HasValue)
        {
            return null;
        }

        return _client.GetGuild(interaction.GuildId.Value);
    }
}
