using DiscordBot.Domain.Entities;
using DiscordBot.Infrastructure.Data;
using DiscordBot.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace DiscordBot.Infrastructure.Services;

public interface ICommandPanelService
{
    Task<IReadOnlyList<CommandPanelRefreshDto>> GetPendingRefreshesAsync(
        CancellationToken cancellationToken = default);

    Task<bool> AcknowledgeRefreshAsync(
        string discordGuildId,
        AckCommandPanelRequest request,
        CancellationToken cancellationToken = default);

    Task<CommandPanelConfigDto?> GetConfigByDiscordGuildIdAsync(
        string discordGuildId,
        CancellationToken cancellationToken = default);
}

public class CommandPanelService : ICommandPanelService
{
    private readonly AppDbContext _dbContext;

    public CommandPanelService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<CommandPanelRefreshDto>> GetPendingRefreshesAsync(
        CancellationToken cancellationToken = default)
    {
        var guilds = await _dbContext.Guilds
            .AsNoTracking()
            .Include(g => g.Settings)
            .Where(g => g.IsActive
                        && g.Settings != null
                        && g.Settings.CommandPanelEnabled
                        && g.Settings.CommandPanelChannelId != null
                        && g.Settings.CommandPanelChannelId != ""
                        && (g.Settings.CommandPanelRefreshRequested
                            || g.Settings.CommandPanelMessageId == null
                            || g.Settings.CommandPanelMessageId == ""))
            .ToListAsync(cancellationToken);

        return guilds
            .Where(g => g.Settings is not null)
            .Select(g => new CommandPanelRefreshDto
            {
                DiscordGuildId = g.DiscordGuildId,
                Config = MapConfig(g.Settings!)
            })
            .ToList();
    }

    public async Task<bool> AcknowledgeRefreshAsync(
        string discordGuildId,
        AckCommandPanelRequest request,
        CancellationToken cancellationToken = default)
    {
        var guild = await _dbContext.Guilds
            .Include(g => g.Settings)
            .FirstOrDefaultAsync(g => g.DiscordGuildId == discordGuildId && g.IsActive, cancellationToken);

        if (guild?.Settings is null)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(request.MessageId))
        {
            guild.Settings.CommandPanelMessageId = request.MessageId.Trim();
        }

        guild.Settings.CommandPanelRefreshRequested = false;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<CommandPanelConfigDto?> GetConfigByDiscordGuildIdAsync(
        string discordGuildId,
        CancellationToken cancellationToken = default)
    {
        var guild = await _dbContext.Guilds
            .AsNoTracking()
            .Include(g => g.Settings)
            .FirstOrDefaultAsync(g => g.DiscordGuildId == discordGuildId && g.IsActive, cancellationToken);

        return guild?.Settings is null ? null : MapConfig(guild.Settings);
    }

    internal static CommandPanelConfigDto MapConfig(GuildSettings settings) =>
        new()
        {
            Enabled = settings.CommandPanelEnabled,
            ChannelId = settings.CommandPanelChannelId,
            MessageId = settings.CommandPanelMessageId,
            Title = settings.CommandPanelTitle,
            Description = settings.CommandPanelDescription,
            Buttons = CommandPanelSerializer.ParseButtons(settings.CommandPanelButtonsJson)
        };

    internal static bool ShouldRequestRefresh(
        GuildSettings settings,
        UpdateGuildSettingsRequest request)
    {
        var normalizedButtons = CommandPanelSerializer.SerializeButtons(request.CommandPanelButtons);

        return settings.CommandPanelEnabled != request.CommandPanelEnabled
               || settings.CommandPanelChannelId != request.CommandPanelChannelId
               || settings.CommandPanelTitle != request.CommandPanelTitle.Trim()
               || settings.CommandPanelDescription != request.CommandPanelDescription.Trim()
               || settings.CommandPanelButtonsJson != normalizedButtons;
    }
}
