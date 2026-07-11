using DiscordBot.Activities.Application.Models;

namespace DiscordBot.Activities.Application.Abstractions;

public interface IPlatformApiClient
{
    Task<GameAccessResult> ValidateGameAccessAsync(ValidateGameAccessRequest request, CancellationToken cancellationToken = default);
    Task<WalletReservationResult> ReserveWalletAsync(ReserveWalletRequest request, CancellationToken cancellationToken = default);
    Task CommitWalletReservationAsync(string reservationId, CancellationToken cancellationToken = default);
    Task ReleaseWalletReservationAsync(string reservationId, CancellationToken cancellationToken = default);
    Task<WalletCreditResult> CreditWalletAsync(WalletCreditRequest request, CancellationToken cancellationToken = default);
}
