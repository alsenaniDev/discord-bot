using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DiscordBot.Bot.Api.Models;
using DiscordBot.Bot.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DiscordBot.Bot.Api;

/// <summary>
/// HTTP client for calling the .NET API. The bot never accesses the database directly.
/// </summary>
public class BotApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<BotApiClient> _logger;

    public BotApiClient(HttpClient httpClient, IOptions<ApiOptions> apiOptions, ILogger<BotApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        var options = apiOptions.Value;
        _httpClient.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
        _httpClient.DefaultRequestHeaders.Add("X-Bot-Api-Key", options.ApiKey);
    }

    public async Task<RegisterGuildResponse?> RegisterGuildAsync(
        RegisterGuildRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "api/bot/guilds/join",
                request,
                JsonOptions,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to register guild {GuildId}. Status: {Status}",
                    request.DiscordGuildId,
                    response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<RegisterGuildResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering guild {GuildId}", request.DiscordGuildId);
            return null;
        }
    }

    public async Task<GuildSettingsResponse?> GetSettingsAsync(
        string discordGuildId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"api/bot/guilds/{discordGuildId}/settings",
                cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to get settings for guild {GuildId}. Status: {Status}",
                    discordGuildId,
                    response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<GuildSettingsResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching settings for guild {GuildId}", discordGuildId);
            return null;
        }
    }

    public async Task<bool> SetupTicketsAsync(
        string discordGuildId,
        string ticketCategoryId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"api/bot/guilds/{discordGuildId}/tickets/setup",
                new SetupTicketsApiRequest { TicketCategoryId = ticketCategoryId },
                JsonOptions,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to setup tickets for guild {GuildId}. Status: {Status}",
                    discordGuildId,
                    response.StatusCode);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting up tickets for guild {GuildId}", discordGuildId);
            return false;
        }
    }

    public async Task<(TicketResponse? Ticket, string? ErrorMessage)> CreateTicketAsync(
        CreateTicketApiRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "api/bot/tickets",
                request,
                JsonOptions,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var ticket = await response.Content.ReadFromJsonAsync<TicketResponse>(JsonOptions, cancellationToken);
                return (ticket, null);
            }

            var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(JsonOptions, cancellationToken);
            return (null, error?.Message ?? "Failed to create ticket.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error creating ticket for guild {GuildId}", request.DiscordGuildId);
            return (null, "Could not reach the API. Make sure the API is running.");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid API response when creating ticket for guild {GuildId}", request.DiscordGuildId);
            return (null, "Received an invalid response from the API.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating ticket for guild {GuildId}", request.DiscordGuildId);
            return (null, "Could not create the ticket.");
        }
    }

    public async Task<TicketResponse?> GetTicketByChannelAsync(
        string channelDiscordId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"api/bot/tickets/by-channel/{channelDiscordId}",
                cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<TicketResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching ticket for channel {ChannelId}", channelDiscordId);
            return null;
        }
    }

    public async Task<TicketResponse?> CloseTicketAsync(
        Guid ticketId,
        CloseTicketApiRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PatchAsJsonAsync(
                $"api/bot/tickets/{ticketId}/close",
                request ?? new CloseTicketApiRequest(),
                JsonOptions,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<TicketResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing ticket {TicketId}", ticketId);
            return null;
        }
    }

    public async Task<bool> SyncResourcesAsync(
        string discordGuildId,
        SyncResourcesApiRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"api/bot/guilds/{discordGuildId}/resources",
                request,
                JsonOptions,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to sync resources for guild {GuildId}. Status: {Status}",
                    discordGuildId,
                    response.StatusCode);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing resources for guild {GuildId}", discordGuildId);
            return false;
        }
    }

    public async Task<IReadOnlyList<string>> GetPendingSyncRequestsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                "api/bot/guilds/sync-requests",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return [];
            }

            return await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions, cancellationToken) ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching pending resource sync requests.");
            return [];
        }
    }

    public async Task<EvaluatePermissionsApiResponse?> EvaluatePermissionsAsync(
        string discordGuildId,
        string discordUserId,
        IReadOnlyList<string> discordRoleIds,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"api/bot/guilds/{discordGuildId}/permissions/evaluate",
                new EvaluatePermissionsApiRequest
                {
                    DiscordUserId = discordUserId,
                    DiscordRoleIds = discordRoleIds.ToList()
                },
                JsonOptions,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<EvaluatePermissionsApiResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating permissions for guild {GuildId}", discordGuildId);
            return null;
        }
    }

    public async Task<GuildProfileApiResponse?> GetGuildProfileAsync(
        string discordGuildId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"api/bot/guilds/{discordGuildId}/profile",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<GuildProfileApiResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching guild profile for {GuildId}", discordGuildId);
            return null;
        }
    }

    public async Task<EvaluateDashboardAccessApiResponse?> EvaluateDashboardAccessAsync(
        string discordGuildId,
        string discordUserId,
        IReadOnlyList<string> discordRoleIds,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"api/bot/guilds/{discordGuildId}/dashboard-access/evaluate",
                new EvaluatePermissionsApiRequest
                {
                    DiscordUserId = discordUserId,
                    DiscordRoleIds = discordRoleIds.ToList()
                },
                JsonOptions,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<EvaluateDashboardAccessApiResponse>(
                JsonOptions,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating dashboard access for guild {GuildId}", discordGuildId);
            return null;
        }
    }

    public async Task<WarningApiResponse?> CreateWarningAsync(
        CreateWarningApiRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "api/bot/moderation/warnings",
                request,
                JsonOptions,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to create warning for guild {GuildId}. Status: {Status}",
                    request.DiscordGuildId,
                    response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<WarningApiResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating warning for guild {GuildId}", request.DiscordGuildId);
            return null;
        }
    }

    public async Task<bool> CreateModerationCaseAsync(
        CreateModerationCaseApiRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "api/bot/moderation/cases",
                request,
                JsonOptions,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to create moderation case for guild {GuildId}. Status: {Status}",
                    request.DiscordGuildId,
                    response.StatusCode);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating moderation case for guild {GuildId}", request.DiscordGuildId);
            return false;
        }
    }

    public async Task<IReadOnlyList<WarningApiResponse>> GetWarningsAsync(
        string discordGuildId,
        string targetUserId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"api/bot/moderation/warnings?discordGuildId={Uri.EscapeDataString(discordGuildId)}&targetUserId={Uri.EscapeDataString(targetUserId)}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return [];
            }

            return await response.Content.ReadFromJsonAsync<List<WarningApiResponse>>(JsonOptions, cancellationToken) ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching warnings for guild {GuildId}", discordGuildId);
            return [];
        }
    }

    public async Task<GuildModuleStatusResponse?> GetModuleStatusAsync(
        string discordGuildId,
        string moduleKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"api/bot/guilds/{discordGuildId}/modules/{Uri.EscapeDataString(moduleKey)}",
                cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to get module status for guild {GuildId}, module {ModuleKey}. Status: {Status}",
                    discordGuildId,
                    moduleKey,
                    response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<GuildModuleStatusResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error fetching module status for guild {GuildId}, module {ModuleKey}",
                discordGuildId,
                moduleKey);
            return null;
        }
    }

    public async Task<bool> IsModuleEnabledAsync(
        string discordGuildId,
        string moduleKey,
        CancellationToken cancellationToken = default)
    {
        var status = await GetModuleStatusAsync(discordGuildId, moduleKey, cancellationToken);
        return status is { IsEnabled: true, AllowedByPlan: true };
    }

    public async Task<bool> CreateLogAsync(
        CreateLogApiRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "api/bot/logs",
                request,
                JsonOptions,
                cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return true;
            }

            if (response.StatusCode != System.Net.HttpStatusCode.Accepted)
            {
                _logger.LogWarning(
                    "Failed to create log for guild {GuildId}. Status: {Status}",
                    request.DiscordGuildId,
                    response.StatusCode);
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating log for guild {GuildId}", request.DiscordGuildId);
            return false;
        }
    }

    public async Task<ReactionRoleApiResponse?> CreateReactionRoleAsync(
        CreateReactionRoleApiRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "api/bot/reaction-roles",
                request,
                JsonOptions,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to create reaction role for guild {GuildId}. Status: {Status}",
                    request.DiscordGuildId,
                    response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ReactionRoleApiResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating reaction role for guild {GuildId}", request.DiscordGuildId);
            return null;
        }
    }

    public async Task<ReactionRoleApiResponse?> GetReactionRoleByButtonAsync(
        string buttonCustomId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"api/bot/reaction-roles/by-button/{Uri.EscapeDataString(buttonCustomId)}",
                cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to get reaction role for button {CustomId}. Status: {Status}",
                    buttonCustomId,
                    response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ReactionRoleApiResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching reaction role for button {CustomId}", buttonCustomId);
            return null;
        }
    }

    public async Task<IReadOnlyList<CommandPanelRefreshApiResponse>> GetPendingCommandPanelRefreshesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                "api/bot/command-panels/pending",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return [];
            }

            return await response.Content.ReadFromJsonAsync<List<CommandPanelRefreshApiResponse>>(JsonOptions, cancellationToken) ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching pending command panel refreshes.");
            return [];
        }
    }

    public async Task AckCommandPanelAsync(
        string discordGuildId,
        AckCommandPanelApiRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"api/bot/command-panels/{discordGuildId}/ack",
                request,
                JsonOptions,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to acknowledge command panel refresh for guild {GuildId}. Status: {Status}",
                    discordGuildId,
                    response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acknowledging command panel refresh for guild {GuildId}", discordGuildId);
        }
    }

    public async Task<IReadOnlyList<TicketCleanupApiResponse>> GetPendingTicketCleanupsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                "api/bot/tickets/pending-cleanups",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return [];
            }

            return await response.Content.ReadFromJsonAsync<List<TicketCleanupApiResponse>>(JsonOptions, cancellationToken) ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching pending ticket channel cleanups.");
            return [];
        }
    }

    public async Task AckTicketCleanupAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsync(
                $"api/bot/tickets/{ticketId}/ack-cleanup",
                null,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to acknowledge ticket cleanup for ticket {TicketId}. Status: {Status}",
                    ticketId,
                    response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acknowledging ticket cleanup for ticket {TicketId}", ticketId);
        }
    }

    public async Task<IReadOnlyList<AutoReplyRuleApiResponse>> GetAutoReplyRulesAsync(
        string discordGuildId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"api/bot/guilds/{discordGuildId}/auto-replies",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return [];
            }

            return await response.Content.ReadFromJsonAsync<List<AutoReplyRuleApiResponse>>(JsonOptions, cancellationToken) ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching auto-reply rules for guild {GuildId}", discordGuildId);
            return [];
        }
    }

    public async Task<IReadOnlyList<PendingTicketMessageApiResponse>> GetPendingTicketMessagesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                "api/bot/tickets/pending-messages",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return [];
            }

            return await response.Content.ReadFromJsonAsync<List<PendingTicketMessageApiResponse>>(JsonOptions, cancellationToken) ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching pending ticket outbound messages.");
            return [];
        }
    }

    public async Task AckTicketMessageAsync(
        Guid messageId,
        bool delivered = true,
        string? failureReason = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"api/bot/tickets/messages/{messageId}/ack",
                new AcknowledgeTicketMessageDeliveryApiRequest
                {
                    Delivered = delivered,
                    FailureReason = failureReason
                },
                JsonOptions,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to acknowledge ticket message {MessageId}. Status: {Status}",
                    messageId,
                    response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acknowledging ticket message {MessageId}", messageId);
        }
    }

    public async Task RecordTicketMessageSentAsync(
        RecordTicketMessageSentApiRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "api/bot/tickets/timeline/message-sent",
                request,
                JsonOptions,
                cancellationToken);

            if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning(
                    "Failed to record ticket message for channel {ChannelId}. Status: {Status}",
                    request.ChannelDiscordId,
                    response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording ticket timeline message for channel {ChannelId}", request.ChannelDiscordId);
        }
    }

    public async Task<PaginatedTicketConversationApiResponse?> GetTicketConversationAsync(
        Guid ticketId,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = limit is > 0
                ? $"api/bot/tickets/{ticketId}/conversation?limit={limit.Value}"
                : $"api/bot/tickets/{ticketId}/conversation";

            var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<PaginatedTicketConversationApiResponse>(
                JsonOptions,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching ticket conversation for ticket {TicketId}", ticketId);
            return null;
        }
    }

    public async Task<IReadOnlyList<TicketTimelineEventApiResponse>> GetTicketTimelineAsync(
        Guid ticketId,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = limit is > 0
                ? $"api/bot/tickets/{ticketId}/timeline?limit={limit.Value}"
                : $"api/bot/tickets/{ticketId}/timeline";

            var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return [];
            }

            return await response.Content.ReadFromJsonAsync<List<TicketTimelineEventApiResponse>>(JsonOptions, cancellationToken) ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching ticket timeline for ticket {TicketId}", ticketId);
            return [];
        }
    }

    public async Task RecordTicketArchivePostedAsync(
        Guid ticketId,
        RecordTicketArchivePostedApiRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"api/bot/tickets/{ticketId}/timeline/archive-posted",
                request,
                JsonOptions,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to record archive timeline event for ticket {TicketId}. Status: {Status}",
                    ticketId,
                    response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording archive timeline event for ticket {TicketId}", ticketId);
        }
    }
}

public sealed class ApiErrorResponse
{
    public string? Message { get; set; }
}
