using Discord;
using Discord.WebSocket;
using DiscordBot.Bot.Services;
using DiscordBot.Bot.UI;
using DiscordBot.Domain.Constants;

namespace DiscordBot.Bot.Commands;

public class TicketInteractionHandlers
{
    private readonly TicketCommandHandlers _ticketHandlers;
    private readonly EmbedBuilderService _embeds;
    private readonly ComponentBuilderService _components;
    private readonly ModuleGuard _moduleGuard;

    public TicketInteractionHandlers(
        TicketCommandHandlers ticketHandlers,
        EmbedBuilderService embeds,
        ComponentBuilderService components,
        ModuleGuard moduleGuard)
    {
        _ticketHandlers = ticketHandlers;
        _embeds = embeds;
        _components = components;
        _moduleGuard = moduleGuard;
    }

    public async Task HandleButtonAsync(SocketMessageComponent component)
    {
        if (component.Data.CustomId == DiscordCustomIds.TicketCreate)
        {
            await HandleCreateButtonAsync(component);
            return;
        }

        if (DiscordCustomIds.TryParseChannelId(
                component.Data.CustomId,
                DiscordCustomIds.TicketClosePrefix,
                out var channelId))
        {
            await HandleCloseButtonAsync(component, channelId);
        }
    }

    public async Task HandleSelectMenuAsync(SocketMessageComponent component)
    {
        if (!DiscordCustomIds.TryParseChannelId(
                component.Data.CustomId,
                DiscordCustomIds.TicketSelectPrefix,
                out var channelId))
        {
            return;
        }

        var selected = component.Data.Values.FirstOrDefault();
        switch (selected)
        {
            case DiscordCustomIds.TicketSelectClose:
                await HandleCloseButtonAsync(component, channelId);
                break;
            case DiscordCustomIds.TicketSelectHelp:
                await component.RespondAsync(embed: _embeds.BuildTicketHelp(), ephemeral: true);
                break;
        }
    }

    public async Task HandleCloseModalAsync(SocketModal modal)
    {
        if (!DiscordCustomIds.TryParseChannelId(
                modal.Data.CustomId,
                DiscordCustomIds.TicketCloseModalPrefix,
                out var channelId))
        {
            return;
        }

        var confirmation = modal.Data.Components
            .FirstOrDefault(c => c.CustomId == "confirmation")
            ?.Value;

        if (!string.Equals(confirmation, "CLOSE", StringComparison.OrdinalIgnoreCase))
        {
            await InteractionResponseHelper.RespondErrorAsync(
                modal,
                _embeds,
                "Confirmation failed",
                "Type **CLOSE** exactly to confirm closing this ticket.");
            return;
        }

        if (modal.Channel is not ITextChannel channel || channel.Id != channelId)
        {
            await InteractionResponseHelper.RespondErrorAsync(
                modal,
                _embeds,
                "Wrong channel",
                "This confirmation must be submitted from the ticket channel.");
            return;
        }

        var guild = (modal.User as SocketGuildUser)?.Guild;
        var user = modal.User as SocketGuildUser;
        if (guild is null || user is null)
        {
            await InteractionResponseHelper.RespondErrorAsync(
                modal,
                _embeds,
                "Unavailable",
                "Could not verify your server membership.");
            return;
        }

        if (!await _moduleGuard.EnsureEnabledForInteractionAsync(
                modal,
                guild.Id.ToString(),
                ModuleKeys.Tickets))
        {
            return;
        }

        await modal.DeferAsync();

        var result = await _ticketHandlers.CloseTicketAsync(guild, channel, user);
        if (!result.Success)
        {
            await InteractionResponseHelper.FollowupErrorAsync(
                modal,
                _embeds,
                result.ErrorTitle ?? "Could not close ticket",
                result.ErrorDescription ?? "Please try again.");
            return;
        }

        await InteractionResponseHelper.FollowupSuccessAsync(
            modal,
            _embeds,
            "Ticket closed",
            $"Ticket #{result.TicketNumber} has been closed.");
    }

    private async Task HandleCreateButtonAsync(SocketMessageComponent component)
    {
        var guild = (component.User as SocketGuildUser)?.Guild;
        var user = component.User as SocketGuildUser;
        if (guild is null || user is null)
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
                ModuleKeys.Tickets))
        {
            return;
        }

        await component.DeferAsync(ephemeral: true);

        var result = await _ticketHandlers.CreateTicketAsync(guild, user);
        if (!result.Success || result.Channel is null)
        {
            await InteractionResponseHelper.FollowupErrorAsync(
                component,
                _embeds,
                result.ErrorTitle ?? "Could not create ticket",
                result.ErrorDescription ?? "Please try again.");
            return;
        }

        await component.FollowupAsync(embed: _embeds.BuildTicketCreated(result.Channel, result.TicketNumber));
    }

    private async Task HandleCloseButtonAsync(SocketMessageComponent component, ulong channelId)
    {
        if (component.Channel is not ITextChannel channel || channel.Id != channelId)
        {
            await InteractionResponseHelper.RespondErrorAsync(
                component,
                _embeds,
                "Wrong channel",
                "Use the close controls inside your ticket channel.");
            return;
        }

        var user = component.User as SocketGuildUser;
        if (user is null)
        {
            await InteractionResponseHelper.RespondErrorAsync(
                component,
                _embeds,
                "Member not found",
                "Could not resolve your membership in this server.");
            return;
        }

        var guild = user.Guild;
        if (!await _moduleGuard.EnsureEnabledForInteractionAsync(
                component,
                guild.Id.ToString(),
                ModuleKeys.Tickets))
        {
            return;
        }

        var accessError = await _ticketHandlers.ValidateCloseAccessAsync(channel, user);
        if (accessError is not null)
        {
            await InteractionResponseHelper.RespondErrorAsync(
                component,
                _embeds,
                accessError.Value.Title,
                accessError.Value.Description,
                ephemeral: true);
            return;
        }

        await component.RespondWithModalAsync(_components.BuildCloseTicketModal(channelId));
    }
}
