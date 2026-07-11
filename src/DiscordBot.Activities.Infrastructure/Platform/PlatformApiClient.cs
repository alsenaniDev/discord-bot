using System.Net.Http.Json;
using DiscordBot.Activities.Application.Abstractions;
using DiscordBot.Activities.Application.Models;
using DiscordBot.Activities.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace DiscordBot.Activities.Infrastructure.Platform;

public class PlatformApiClient(HttpClient http, IOptions<PlatformApiOptions> options) : IPlatformApiClient
{
    public async Task<GameAccessResult> ValidateGameAccessAsync(ValidateGameAccessRequest request, CancellationToken cancellationToken = default)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, "api/internal/activities/game-access/validate")
        {
            Content = JsonContent.Create(request)
        };
        AddServiceAuth(message);
        using var response = await http.SendAsync(message, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return new GameAccessResult { Allowed = false, GameKey = request.GameKey, DenialReason = "تعذر التحقق من صلاحية اللعبة من منصة البوت." };
        }

        return await response.Content.ReadFromJsonAsync<GameAccessResult>(cancellationToken: cancellationToken)
            ?? new GameAccessResult { Allowed = false, GameKey = request.GameKey, DenialReason = "استجابة منصة البوت غير صالحة." };
    }

    public async Task<WalletReservationResult> ReserveWalletAsync(ReserveWalletRequest request, CancellationToken cancellationToken = default)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, "api/internal/activities/wallet/reservations") { Content = JsonContent.Create(request) };
        AddServiceAuth(message);
        using var response = await http.SendAsync(message, cancellationToken);
        if (!response.IsSuccessStatusCode) return new WalletReservationResult { Succeeded = false, ErrorMessage = "تعذر حجز الرصيد." };
        return await response.Content.ReadFromJsonAsync<WalletReservationResult>(cancellationToken: cancellationToken) ?? new WalletReservationResult { Succeeded = false, ErrorMessage = "استجابة حجز الرصيد غير صالحة." };
    }

    public Task CommitWalletReservationAsync(string reservationId, CancellationToken cancellationToken = default) =>
        SendReservationActionAsync(reservationId, "commit", cancellationToken);

    public Task ReleaseWalletReservationAsync(string reservationId, CancellationToken cancellationToken = default) =>
        SendReservationActionAsync(reservationId, "release", cancellationToken);

    public async Task<WalletCreditResult> CreditWalletAsync(WalletCreditRequest request, CancellationToken cancellationToken = default)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, "api/internal/activities/wallet/credits") { Content = JsonContent.Create(request) };
        AddServiceAuth(message);
        using var response = await http.SendAsync(message, cancellationToken);
        if (!response.IsSuccessStatusCode) return new WalletCreditResult { Succeeded = false, Status = "Failed", ErrorMessage = "تعذر إضافة مكافأة المحفظة." };
        return await response.Content.ReadFromJsonAsync<WalletCreditResult>(cancellationToken: cancellationToken) ?? new WalletCreditResult { Succeeded = false, Status = "Failed", ErrorMessage = "استجابة إضافة الرصيد غير صالحة." };
    }

    private async Task SendReservationActionAsync(string reservationId, string action, CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, $"api/internal/activities/wallet/reservations/{Uri.EscapeDataString(reservationId)}/{action}");
        AddServiceAuth(message);
        using var response = await http.SendAsync(message, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private void AddServiceAuth(HttpRequestMessage message)
    {
        var token = options.Value.ServiceToken;
        if (!string.IsNullOrWhiteSpace(token)) message.Headers.Add("X-Activities-Service-Key", token);
    }
}
