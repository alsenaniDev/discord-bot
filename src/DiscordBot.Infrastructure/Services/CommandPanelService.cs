using DiscordBot.Domain.Entities;
using DiscordBot.Domain.Enums;
using DiscordBot.Infrastructure.Data;
using DiscordBot.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DiscordBot.Infrastructure.Services;

public interface ICommandPanelService
{
    Task<IReadOnlyList<GuildPanelDto>> GetGuildPanelsAsync(Guid guildId, CancellationToken cancellationToken = default);
    Task<GuildPanelDto?> GetGuildPanelAsync(Guid guildId, Guid panelId, CancellationToken cancellationToken = default);
    Task<(GuildPanelDto? Panel, string? Error)> CreateAsync(Guid guildId, SaveGuildPanelRequest request, CancellationToken cancellationToken = default);
    Task<(GuildPanelDto? Panel, string? Error)> UpdateAsync(Guid guildId, Guid panelId, SaveGuildPanelRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid guildId, Guid panelId, CancellationToken cancellationToken = default);
    Task<(bool Found, string? Error)> RequestPublishAsync(Guid guildId, Guid panelId, CancellationToken cancellationToken = default);
    Task<bool> UnpublishAsync(Guid guildId, Guid panelId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CommandPanelRefreshDto>> GetPendingRefreshesAsync(CancellationToken cancellationToken = default);
    Task<bool> AcknowledgeRefreshAsync(Guid panelId, AckCommandPanelRequest request, CancellationToken cancellationToken = default);
    Task<PanelButtonActionDto?> GetButtonActionAsync(Guid panelId, Guid buttonId, CancellationToken cancellationToken = default);
}

public class CommandPanelService : ICommandPanelService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<CommandPanelService> _logger;

    public CommandPanelService(AppDbContext dbContext, ILogger<CommandPanelService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IReadOnlyList<GuildPanelDto>> GetGuildPanelsAsync(Guid guildId, CancellationToken cancellationToken = default)
    {
        var panels = await _dbContext.GuildPanels.AsNoTracking().Include(x => x.Buttons)
            .Where(x => x.GuildId == guildId).OrderBy(x => x.Name).ToListAsync(cancellationToken);
        return panels.Select(Map).ToList();
    }

    public async Task<GuildPanelDto?> GetGuildPanelAsync(Guid guildId, Guid panelId, CancellationToken cancellationToken = default)
    {
        var panel = await _dbContext.GuildPanels.AsNoTracking().Include(x => x.Buttons)
            .FirstOrDefaultAsync(x => x.GuildId == guildId && x.Id == panelId, cancellationToken);
        return panel is null ? null : Map(panel);
    }

    public async Task<(GuildPanelDto? Panel, string? Error)> CreateAsync(Guid guildId, SaveGuildPanelRequest request, CancellationToken cancellationToken = default)
    {
        var error = await ValidateAsync(guildId, request, cancellationToken);
        if (error is not null) return (null, error);

        var panel = new GuildPanel { GuildId = guildId };
        Apply(panel, request);
        _dbContext.GuildPanels.Add(panel);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return (Map(panel), null);
    }

    public async Task<(GuildPanelDto? Panel, string? Error)> UpdateAsync(Guid guildId, Guid panelId, SaveGuildPanelRequest request, CancellationToken cancellationToken = default)
    {
        var panel = await _dbContext.GuildPanels.Include(x => x.Buttons)
            .FirstOrDefaultAsync(x => x.GuildId == guildId && x.Id == panelId, cancellationToken);
        if (panel is null) return (null, null);
        var error = await ValidateAsync(guildId, request, cancellationToken);
        if (error is not null) return (null, error);

        Apply(panel, request);
        // Saving is intentionally separate from publishing. Only the publish endpoint queues Discord work.
        panel.RefreshRequested = false;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return (Map(panel), null);
    }

    public async Task<bool> DeleteAsync(Guid guildId, Guid panelId, CancellationToken cancellationToken = default)
    {
        var panel = await _dbContext.GuildPanels.FirstOrDefaultAsync(x => x.GuildId == guildId && x.Id == panelId, cancellationToken);
        if (panel is null) return false;
        _dbContext.GuildPanels.Remove(panel);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<(bool Found, string? Error)> RequestPublishAsync(Guid guildId, Guid panelId, CancellationToken cancellationToken = default)
    {
        var panel = await _dbContext.GuildPanels.Include(x => x.Buttons)
            .FirstOrDefaultAsync(x => x.GuildId == guildId && x.Id == panelId, cancellationToken);
        if (panel is null) return (false, null);
        if (!panel.IsEnabled) return (true, "Disabled panels cannot be published. Enable and save the panel first.");
        if (string.IsNullOrWhiteSpace(panel.ChannelDiscordId)) return (true, "Select a Discord channel before publishing.");
        if (!await _dbContext.DiscordChannels.AsNoTracking().AnyAsync(
                x => x.GuildId == guildId && x.DiscordChannelId == panel.ChannelDiscordId
                    && x.Type == DiscordChannelType.Text, cancellationToken))
            return (true, "The selected destination is not an available text channel. Sync resources and choose a text channel.");
        if (!panel.Buttons.Any(x => x.IsEnabled)) return (true, "Enable at least one button before publishing.");
        if (string.IsNullOrWhiteSpace(panel.MessageDiscordId)) panel.IsPublished = false;
        panel.RefreshRequested = true;
        panel.LastPublishFailed = false;
        panel.LastPublishFailureReason = null;
        panel.LastPublishAttemptedAtUtc = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Panel {PanelId} queued for Discord publishing.", panelId);
        return (true, null);
    }

    public async Task<bool> UnpublishAsync(Guid guildId, Guid panelId, CancellationToken cancellationToken = default)
    {
        var panel = await _dbContext.GuildPanels.FirstOrDefaultAsync(x => x.GuildId == guildId && x.Id == panelId, cancellationToken);
        if (panel is null) return false;
        // The bot cannot delete a message through the polling contract yet; disabling prevents future interaction resolution.
        panel.IsEnabled = false;
        panel.IsPublished = false;
        panel.RefreshRequested = false;
        panel.LastPublishFailed = false;
        panel.LastPublishFailureReason = null;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<CommandPanelRefreshDto>> GetPendingRefreshesAsync(CancellationToken cancellationToken = default)
    {
        var panels = await _dbContext.GuildPanels.AsNoTracking().Include(x => x.Guild).Include(x => x.Buttons)
            .Where(x => x.Guild.IsActive && x.IsEnabled && x.RefreshRequested && x.ChannelDiscordId != ""
                && x.Buttons.Any(button => button.IsEnabled))
            .OrderBy(x => x.CreatedAt).ToListAsync(cancellationToken);
        var pending = panels.Select(x => new CommandPanelRefreshDto
        {
            PanelId = x.Id,
            DiscordGuildId = x.Guild.DiscordGuildId,
            ChannelDiscordId = x.ChannelDiscordId,
            MessageDiscordId = x.MessageDiscordId,
            Title = x.Title,
            Description = x.Description,
            ImageUrl = x.ImageUrl,
            Buttons = x.Buttons.Where(b => b.IsEnabled).OrderBy(b => b.SortOrder).Select(MapButton).ToList()
        }).ToList();
        _logger.LogInformation("Returning {Count} pending guild panel(s) to the bot.", pending.Count);
        return pending;
    }

    public async Task<bool> AcknowledgeRefreshAsync(Guid panelId, AckCommandPanelRequest request, CancellationToken cancellationToken = default)
    {
        var panel = await _dbContext.GuildPanels.FirstOrDefaultAsync(x => x.Id == panelId, cancellationToken);
        if (panel is null) return false;
        panel.RefreshRequested = false;
        panel.LastPublishAttemptedAtUtc ??= DateTimeOffset.UtcNow;
        if (request.Success && !string.IsNullOrWhiteSpace(request.MessageDiscordId))
        {
            panel.IsPublished = true;
            panel.MessageDiscordId = request.MessageDiscordId.Trim();
            panel.LastPublishedAtUtc = DateTimeOffset.UtcNow;
            panel.LastPublishFailed = false;
            panel.LastPublishFailureReason = null;
            _logger.LogInformation("Panel {PanelId} publishing acknowledged.", panelId);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(panel.MessageDiscordId)) panel.IsPublished = false;
            panel.LastPublishFailed = true;
            var reason = request.Success
                ? "The bot acknowledged success without a Discord message ID."
                : request.FailureReason;
            panel.LastPublishFailureReason = string.IsNullOrWhiteSpace(reason)
                ? "Discord publishing failed without an error message."
                : reason.Trim()[..Math.Min(reason.Trim().Length, 1000)];
            _logger.LogWarning("Panel {PanelId} publishing failed: {FailureReason}", panelId, panel.LastPublishFailureReason);
        }
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<PanelButtonActionDto?> GetButtonActionAsync(Guid panelId, Guid buttonId, CancellationToken cancellationToken = default) =>
        await _dbContext.GuildPanelButtons.AsNoTracking()
            .Where(x => x.PanelId == panelId && x.Id == buttonId && x.IsEnabled && x.Panel.IsEnabled)
            .Select(x => new PanelButtonActionDto { DiscordGuildId = x.Panel.Guild.DiscordGuildId, PanelId = panelId, ButtonId = buttonId, ActionType = x.ActionType, TicketTypeId = x.TicketTypeId, WorkflowId = x.WorkflowId, Url = x.Url, ResponseMessage = x.ResponseMessage, RoleDiscordId = x.RoleDiscordId })
            .FirstOrDefaultAsync(cancellationToken);

    private async Task<string?> ValidateAsync(Guid guildId, SaveGuildPanelRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) return "Panel name is required.";
        if (request.Name.Trim().Length > 100) return "Panel name must be 100 characters or less.";
        if (string.IsNullOrWhiteSpace(request.Title) && string.IsNullOrWhiteSpace(request.Description)) return "Enter a panel title or description.";
        if ((request.Title?.Trim().Length ?? 0) > 256) return "Panel title must be 256 characters or less.";
        if ((request.Description?.Trim().Length ?? 0) > 2000) return "Panel description must be 2000 characters or less.";
        if (request.Buttons.Count > 25) return "A panel can contain at most 25 buttons (5 per row).";
        if (!string.IsNullOrWhiteSpace(request.ChannelDiscordId)
            && !await _dbContext.DiscordChannels.AsNoTracking().AnyAsync(
                x => x.GuildId == guildId && x.DiscordChannelId == request.ChannelDiscordId
                    && x.Type == DiscordChannelType.Text, cancellationToken))
            return "The selected channel must be a synced text channel in this guild.";
        if (!string.IsNullOrWhiteSpace(request.ImageUrl) && (!Uri.TryCreate(request.ImageUrl, UriKind.Absolute, out var imageUri) || (imageUri.Scheme != "http" && imageUri.Scheme != "https")))
            return "Image URL must be a valid HTTP or HTTPS URL.";
        foreach (var button in request.Buttons)
        {
            if (string.IsNullOrWhiteSpace(button.Label)) return "Every button must have a label.";
            if (button.Label.Trim().Length > 80) return "Button labels must be 80 characters or less.";
            if (button.ActionType == PanelButtonActionType.OpenUrl)
            {
                if (button.Style != PanelButtonStyle.Link) return "Open URL buttons must use the Link style.";
                if (!Uri.TryCreate(button.Url, UriKind.Absolute, out var url) || url.Scheme != Uri.UriSchemeHttps)
                    return "Open URL buttons require a valid HTTPS URL.";
            }
            else if (button.Style == PanelButtonStyle.Link) return "Link style can only be used with the Open URL action.";
            if (button.ActionType == PanelButtonActionType.SendMessage && string.IsNullOrWhiteSpace(button.ResponseMessage))
                return "Send Message buttons require a response message.";
            if (button.ActionType == PanelButtonActionType.AssignRole)
            {
                if (string.IsNullOrWhiteSpace(button.RoleDiscordId)) return "Assign Role buttons require a Discord role.";
                var guildHasSyncedRoles = await _dbContext.DiscordRoles.AsNoTracking().AnyAsync(x => x.GuildId == guildId, cancellationToken);
                if (guildHasSyncedRoles && !await _dbContext.DiscordRoles.AsNoTracking().AnyAsync(
                        x => x.GuildId == guildId && x.DiscordRoleId == button.RoleDiscordId && !x.IsManaged, cancellationToken))
                    return "The selected role is unavailable, managed by Discord, or does not belong to this guild.";
                if (!guildHasSyncedRoles && (!ulong.TryParse(button.RoleDiscordId, out var roleId) || roleId == 0))
                    return "The fallback Discord role ID is invalid.";
            }
            if (button.ActionType == PanelButtonActionType.StartWorkflow)
            {
                if (!button.WorkflowId.HasValue) return "Start Workflow buttons require a workflow.";
                if (!await _dbContext.GuildWorkflows.AsNoTracking().AnyAsync(x => x.Id == button.WorkflowId && x.GuildId == guildId && x.IsEnabled, cancellationToken))
                    return "The selected workflow is unavailable or disabled.";
            }
        }
        return null;
    }

    private static void Apply(GuildPanel panel, SaveGuildPanelRequest request)
    {
        panel.Name = request.Name.Trim(); panel.Title = request.Title?.Trim() ?? string.Empty; panel.Description = request.Description?.Trim() ?? string.Empty;
        panel.ImageUrl = NullIfWhiteSpace(request.ImageUrl); panel.ChannelDiscordId = request.ChannelDiscordId.Trim(); panel.IsEnabled = request.IsEnabled;
        var requestedIds = request.Buttons.Where(x => x.Id.HasValue).Select(x => x.Id!.Value).ToHashSet();
        foreach (var removed in panel.Buttons.Where(x => !requestedIds.Contains(x.Id)).ToList()) panel.Buttons.Remove(removed);
        foreach (var item in request.Buttons.OrderBy(x => x.SortOrder))
        {
            var button = item.Id.HasValue ? panel.Buttons.FirstOrDefault(x => x.Id == item.Id.Value) : null;
            if (button is null) { button = new GuildPanelButton(); panel.Buttons.Add(button); }
            button.Label = item.Label.Trim(); button.Emoji = NullIfWhiteSpace(item.Emoji); button.Style = item.Style;
            button.ActionType = item.ActionType; button.TicketTypeId = item.TicketTypeId; button.WorkflowId = item.WorkflowId; button.Url = NullIfWhiteSpace(item.Url);
            button.ResponseMessage = NullIfWhiteSpace(item.ResponseMessage); button.RoleDiscordId = NullIfWhiteSpace(item.RoleDiscordId);
            button.SortOrder = item.SortOrder; button.IsEnabled = item.IsEnabled;
        }
    }

    private static GuildPanelDto Map(GuildPanel x) => new()
    {
        Id = x.Id, GuildId = x.GuildId, Name = x.Name, Title = x.Title, Description = x.Description, ImageUrl = x.ImageUrl,
        ChannelDiscordId = x.ChannelDiscordId, MessageDiscordId = x.MessageDiscordId, IsEnabled = x.IsEnabled, IsPublished = x.IsPublished,
        RefreshRequested = x.RefreshRequested, PublishStatus = GetPublishStatus(x), LastPublishedAtUtc = x.LastPublishedAtUtc,
        LastPublishFailed = x.LastPublishFailed, LastPublishFailureReason = x.LastPublishFailureReason,
        LastPublishAttemptedAtUtc = x.LastPublishAttemptedAtUtc,
        CreatedAtUtc = x.CreatedAt, UpdatedAtUtc = x.UpdatedAt,
        Buttons = x.Buttons.OrderBy(b => b.SortOrder).Select(MapButton).ToList()
    };
    private static GuildPanelButtonDto MapButton(GuildPanelButton x) => new()
    {
        Id = x.Id, Label = x.Label, Emoji = x.Emoji, Style = x.Style, ActionType = x.ActionType, TicketTypeId = x.TicketTypeId, WorkflowId = x.WorkflowId,
        Url = x.Url, ResponseMessage = x.ResponseMessage, RoleDiscordId = x.RoleDiscordId, SortOrder = x.SortOrder, IsEnabled = x.IsEnabled
    };
    private static PanelPublishStatus GetPublishStatus(GuildPanel panel)
    {
        if (panel.LastPublishFailed || !string.IsNullOrWhiteSpace(panel.LastPublishFailureReason)) return PanelPublishStatus.Failed;
        if (panel.RefreshRequested) return PanelPublishStatus.PendingPublish;
        if (panel.IsPublished && !string.IsNullOrWhiteSpace(panel.MessageDiscordId)) return PanelPublishStatus.Published;
        return PanelPublishStatus.NotPublished;
    }
    private static string? NullIfWhiteSpace(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    // TODO: Remove with the legacy GuildSettings CommandPanel columns after the new panel system is stable.
    internal static bool ShouldRequestRefresh(GuildSettings settings, UpdateGuildSettingsRequest request, string? normalizedImageUrl)
    {
        var normalizedButtons = CommandPanelSerializer.SerializeButtons(request.CommandPanelButtons);
        return settings.CommandPanelEnabled != request.CommandPanelEnabled
            || settings.CommandPanelChannelId != request.CommandPanelChannelId
            || settings.CommandPanelTitle != request.CommandPanelTitle.Trim()
            || settings.CommandPanelDescription != request.CommandPanelDescription.Trim()
            || settings.CommandPanelImageUrl != normalizedImageUrl
            || settings.CommandPanelButtonsJson != normalizedButtons;
    }
}
