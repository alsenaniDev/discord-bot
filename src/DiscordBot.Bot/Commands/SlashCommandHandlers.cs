using Discord.WebSocket;
using DiscordBot.Bot.Api.Models;
using DiscordBot.Bot.Configuration;
using DiscordBot.Bot.Services;
using DiscordBot.Bot.UI;
using Microsoft.Extensions.Options;

namespace DiscordBot.Bot.Commands;

/// <summary>
/// Handles slash command logic. Keeps Discord I/O separate from API calls.
/// </summary>
public class SlashCommandHandlers
{
    private readonly DiscordSocketClient _client;
    private readonly Api.BotApiClient _apiClient;
    private readonly EmbedBuilderService _embeds;
    private readonly ResourceSyncService _resourceSyncService;
    private readonly PlatformOptions _platformOptions;

    public SlashCommandHandlers(
        DiscordSocketClient client,
        Api.BotApiClient apiClient,
        EmbedBuilderService embeds,
        ResourceSyncService resourceSyncService,
        IOptions<PlatformOptions> platformOptions)
    {
        _client = client;
        _apiClient = apiClient;
        _embeds = embeds;
        _resourceSyncService = resourceSyncService;
        _platformOptions = platformOptions.Value;
    }

    public Task HandlePingAsync(SocketInteraction interaction)
    {
        var latency = _client.Latency;
        return interaction.RespondAsync(
            embed: _embeds.BuildPing(latency),
            ephemeral: true);
    }

    public async Task HandleServerAsync(SocketInteraction interaction)
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

        var settings = await _apiClient.GetSettingsAsync(guild.Id.ToString());
        if (settings is null)
        {
            await InteractionResponseHelper.RespondErrorAsync(
                interaction,
                _embeds,
                "Server not registered",
                "This server is not linked to the platform yet. Run `/setup` first.");
            return;
        }

        await interaction.RespondAsync(
            embed: _embeds.BuildServerSettings(guild, settings),
            ephemeral: true);
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

        var iconUrl = guild.IconId is not null
            ? $"https://cdn.discordapp.com/icons/{guild.Id}/{guild.IconId}.png"
            : null;

        var result = await _apiClient.RegisterGuildAsync(new RegisterGuildRequest
        {
            DiscordGuildId = guild.Id.ToString(),
            Name = guild.Name,
            OwnerDiscordUserId = guild.OwnerId.ToString(),
            IconUrl = iconUrl
        });

        if (result is null)
        {
            await InteractionResponseHelper.RespondErrorAsync(
                interaction,
                _embeds,
                "Registration failed",
                "Could not reach the API. Make sure the API is running and the bot API key is configured.");
            return;
        }

        await interaction.DeferAsync(ephemeral: true);

        var resourcesSynced = await _resourceSyncService.SyncGuildAsync(guild);

        await interaction.FollowupAsync(
            embed: _embeds.BuildSetupComplete(result, guild, resourcesSynced, _platformOptions.DashboardUrl),
            ephemeral: true);
    }

    public async Task HandleSyncAsync(SocketInteraction interaction)
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

        await interaction.DeferAsync(ephemeral: true);

        var success = await _resourceSyncService.SyncGuildAsync(guild);
        if (!success)
        {
            await interaction.FollowupAsync(
                embed: _embeds.BuildError(
                    "Sync failed",
                    "Could not reach the API. Make sure the API is running and the bot API key is configured."),
                ephemeral: true);
            return;
        }

        await interaction.FollowupAsync(
            embed: _embeds.BuildSuccess(
                "Resources synced",
                $"Channels and roles for **{guild.Name}** are now up to date."),
            ephemeral: true);
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
