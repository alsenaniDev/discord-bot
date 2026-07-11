using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DiscordBot.Bot.Api.Models;
using DiscordBot.Bot.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DiscordBot.Bot.Api;

public class ActivitiesApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<ActivitiesApiClient> _logger;

    public ActivitiesApiClient(HttpClient httpClient, IOptions<ActivitiesApiOptions> options, ILogger<ActivitiesApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        var value = options.Value;
        _httpClient.BaseAddress = new Uri(value.BaseUrl.TrimEnd('/') + "/");
        if (!string.IsNullOrWhiteSpace(value.ServiceToken)) _httpClient.DefaultRequestHeaders.Add("X-Activities-Service-Key", value.ServiceToken);
    }

    public async Task<IReadOnlyList<PendingActivitiesRouletteAnnouncementApiResponse>> GetPendingRouletteAnnouncementsAsync(CancellationToken ct = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<PendingActivitiesRouletteAnnouncementApiResponse>>("api/internal/bot/roulette/announcements/pending", JsonOptions, ct) ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not load pending Activities Roulette announcements.");
            return [];
        }
    }

    public async Task AckRouletteAnnouncementAsync(Guid gameSessionId, AckActivitiesRouletteAnnouncementApiRequest request, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"api/internal/bot/roulette/announcements/{gameSessionId:D}/ack", request, JsonOptions, ct);
            if (!response.IsSuccessStatusCode) _logger.LogWarning("Activities Roulette announcement ack failed for session {GameSessionId}: {Status}.", gameSessionId, response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not ack Activities Roulette announcement for session {GameSessionId}.", gameSessionId);
        }
    }

    public async Task<(PrepareRouletteJoinApiResponse? Value, string? Error)> PrepareRouletteJoinAsync(Guid gameSessionId, PrepareRouletteJoinApiRequest request, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"api/internal/bot/roulette/sessions/{gameSessionId:D}/prepare-join", request, JsonOptions, ct);
            if (response.IsSuccessStatusCode) return (await response.Content.ReadFromJsonAsync<PrepareRouletteJoinApiResponse>(JsonOptions, ct), null);
            return (null, (await response.Content.ReadFromJsonAsync<ApiErrorResponse>(JsonOptions, ct))?.Message ?? "تعذر تجهيز الانضمام لهذه الجولة.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not prepare Activities Roulette join for session {GameSessionId}, user {UserId}.", gameSessionId, request.UserDiscordId);
            return (null, "تعذر التواصل مع منصة الألعاب الآن.");
        }
    }
}
