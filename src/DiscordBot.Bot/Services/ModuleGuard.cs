using Discord.WebSocket;
using DiscordBot.Bot.Api;
using DiscordBot.Bot.Api.Models;
using DiscordBot.Bot.UI;

namespace DiscordBot.Bot.Services;

/// <summary>
/// Checks whether a platform module is enabled for a guild before running feature logic.
/// </summary>
public class ModuleGuard
{
    private readonly BotApiClient _apiClient;
    private readonly EmbedBuilderService _embeds;

    public ModuleGuard(BotApiClient apiClient, EmbedBuilderService embeds)
    {
        _apiClient = apiClient;
        _embeds = embeds;
    }

    public async Task<bool> IsEnabledAsync(
        string discordGuildId,
        string moduleKey,
        CancellationToken cancellationToken = default)
    {
        var status = await _apiClient.GetModuleStatusAsync(discordGuildId, moduleKey, cancellationToken);
        return status is { IsEnabled: true, AllowedByPlan: true };
    }

    public async Task<bool> EnsureEnabledForInteractionAsync(
        SocketInteraction interaction,
        string discordGuildId,
        string moduleKey,
        CancellationToken cancellationToken = default)
    {
        var status = await _apiClient.GetModuleStatusAsync(discordGuildId, moduleKey, cancellationToken);

        if (status is null || !status.AllowedByPlan)
        {
            await InteractionResponseHelper.RespondInfoAsync(
                interaction,
                _embeds,
                "Plan limit",
                "This module is not available in your current plan.");
            return false;
        }

        if (!status.IsEnabled)
        {
            await InteractionResponseHelper.RespondInfoAsync(
                interaction,
                _embeds,
                "Module disabled",
                "This module is disabled for this server.");
            return false;
        }

        return true;
    }
}
