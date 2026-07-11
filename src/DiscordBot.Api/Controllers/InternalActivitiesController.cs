using System.Text.Json;
using System.Data;
using DiscordBot.Domain.Entities;
using DiscordBot.Infrastructure.Data;
using DiscordBot.Infrastructure.Models;
using DiscordBot.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace DiscordBot.Api.Controllers;

[AllowAnonymous, ApiController, Route("api/internal/activities")]
public class InternalActivitiesController(IGameHubService games, AppDbContext db, IConfiguration configuration, ILogger<InternalActivitiesController> logger) : ControllerBase
{
    [HttpPost("game-access/validate")]
    public async Task<IActionResult> ValidateGameAccess(ValidateActivityGameAccessRequest request, CancellationToken ct)
    {
        if (!Authorized()) return Unauthorized(new { message = "Invalid activities service key." });
        var context = await games.GetActivityContextAsync(request.DiscordGuildId, request.DiscordChannelId, request.DiscordUserId, ct);
        if (!context.Succeeded) return StatusCode(context.StatusCode, new GameAccessResult { Allowed = false, GameKey = request.GameKey, DenialReason = context.Error });
        var game = context.Value!.Games.FirstOrDefault(x => x.Key.Equals(request.GameKey.Trim(), StringComparison.OrdinalIgnoreCase));
        if (game is null) return StatusCode(403, new GameAccessResult { Allowed = false, GameKey = request.GameKey, DenialReason = "هذه اللعبة غير متاحة لهذا السيرفر." });
        logger.LogInformation("Activities game access validated for guild {DiscordGuildId}, user {DiscordUserId}, game {GameKey}, version {Version}, sandbox {IsSandbox}.", request.DiscordGuildId, request.DiscordUserId, game.Key, game.Version, game.IsSandbox);
        var rouletteSettings = game.Key.Equals("roulette", StringComparison.OrdinalIgnoreCase)
            ? await LoadRouletteSettingsAsync(request.DiscordGuildId, ct)
            : null;
        return Ok(new GameAccessResult
        {
            Allowed = true,
            GameKey = game.Key,
            GameVersion = game.Version ?? "1.0.0",
            PlatformGameVersionId = game.GameVersionId,
            Mode = game.IsSandbox ? "Sandbox" : "Production",
            ActivityRoute = game.ActivityRoute,
            SupportsWallet = ManifestBool(game.ManifestJson, "supportsWallet") ?? game.Key.Equals("roulette", StringComparison.OrdinalIgnoreCase),
            RouletteSettings = rouletteSettings
        });
    }

    [HttpPost("wallet/reservations")]
    public async Task<IActionResult> ReserveWallet(ReserveWalletRequest request, CancellationToken ct)
    {
        if (!Authorized()) return Unauthorized(new { message = "Invalid activities service key." });
        if (!ValidSnowflake(request.DiscordGuildId) || !ValidSnowflake(request.DiscordUserId))
            return BadRequest(new WalletReservationResult { Succeeded = false, ErrorMessage = "بيانات Discord غير صالحة." });
        if (request.Amount <= 0)
            return BadRequest(new WalletReservationResult { Succeeded = false, ErrorMessage = "مبلغ الحجز يجب أن يكون أكبر من صفر." });
        if (request.Amount != decimal.Truncate(request.Amount))
            return BadRequest(new WalletReservationResult { Succeeded = false, ErrorMessage = "المحفظة الحالية تدعم عملات صحيحة فقط." });
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
            return BadRequest(new WalletReservationResult { Succeeded = false, ErrorMessage = "مفتاح التكرار مطلوب لحجز الرصيد." });

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var existing = await db.WalletReservations.FirstOrDefaultAsync(x => x.IdempotencyKey == request.IdempotencyKey, ct);
        if (existing is not null)
        {
            await transaction.CommitAsync(ct);
            return Ok(new WalletReservationResult
            {
                Succeeded = existing.Status is "Pending" or "Committed",
                ReservationId = existing.ReservationId,
                Status = existing.Status,
                ErrorMessage = existing.FailureReason
            });
        }

        var guildId = await db.Guilds.Where(x => x.DiscordGuildId == request.DiscordGuildId && x.IsActive).Select(x => (Guid?)x.Id).FirstOrDefaultAsync(ct);
        if (!guildId.HasValue)
            return NotFound(new WalletReservationResult { Succeeded = false, ErrorMessage = "هذا السيرفر غير مربوط بمنصة البوت." });

        var wallet = await db.GameWallets.FirstOrDefaultAsync(x => x.GuildId == guildId && x.UserDiscordId == request.DiscordUserId, ct);
        if (wallet is null)
        {
            wallet = new GameWallet { GuildId = guildId.Value, UserDiscordId = request.DiscordUserId };
            db.GameWallets.Add(wallet);
            await db.SaveChangesAsync(ct);
        }

        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"GameWallets\" SET \"UpdatedAt\" = \"UpdatedAt\" WHERE \"Id\" = {wallet.Id}", ct);
        var now = DateTimeOffset.UtcNow;
        await ExpireWalletReservationsAsync(now, ct);
        var pending = await db.WalletReservations
            .Where(x => x.GuildId == guildId && x.DiscordUserId == request.DiscordUserId && x.Status == "Pending" && x.ExpiresAtUtc > now)
            .SumAsync(x => (decimal?)x.Amount, ct) ?? 0m;
        var available = wallet.Balance - pending;
        if (available < request.Amount)
        {
            var failed = new WalletReservation
            {
                GuildId = guildId.Value,
                DiscordUserId = request.DiscordUserId,
                GameKey = Limit(request.GameKey, 64),
                Amount = request.Amount,
                Currency = Limit(request.Currency, 16),
                IdempotencyKey = Limit(request.IdempotencyKey, 160),
                Status = "Released",
                ExpiresAtUtc = now,
                ReleasedAtUtc = now,
                FailureReason = "رصيدك غير كافٍ لإتمام العملية."
            };
            db.WalletReservations.Add(failed);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return Conflict(new WalletReservationResult { Succeeded = false, ReservationId = failed.ReservationId, Status = failed.Status, ErrorMessage = failed.FailureReason });
        }

        var reservation = new WalletReservation
        {
            GuildId = guildId.Value,
            DiscordUserId = request.DiscordUserId,
            GameKey = Limit(request.GameKey, 64),
            Amount = request.Amount,
            Currency = Limit(request.Currency, 16),
            IdempotencyKey = Limit(request.IdempotencyKey, 160),
            Status = "Pending",
            ExpiresAtUtc = now.AddMinutes(10)
        };
        db.WalletReservations.Add(reservation);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Ok(new WalletReservationResult { Succeeded = true, ReservationId = reservation.ReservationId, Status = reservation.Status });
    }

    [HttpPost("wallet/reservations/{reservationId}/commit")]
    public async Task<IActionResult> CommitWallet(string reservationId, CancellationToken ct)
    {
        if (!Authorized()) return Unauthorized(new { message = "Invalid activities service key." });
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var reservation = await db.WalletReservations.FirstOrDefaultAsync(x => x.ReservationId == reservationId, ct);
        if (reservation is null) return NotFound(new WalletReservationResult { Succeeded = false, ErrorMessage = "حجز الرصيد غير موجود." });
        if (reservation.Status == "Committed")
        {
            await transaction.CommitAsync(ct);
            return Ok(new WalletReservationResult { Succeeded = true, ReservationId = reservation.ReservationId, Status = reservation.Status });
        }
        if (reservation.Status == "Released" || reservation.Status == "Expired")
            return Conflict(new WalletReservationResult { Succeeded = false, ReservationId = reservation.ReservationId, Status = reservation.Status, ErrorMessage = "لا يمكن تأكيد حجز تم إلغاؤه أو انتهت صلاحيته." });

        var now = DateTimeOffset.UtcNow;
        if (reservation.ExpiresAtUtc <= now)
        {
            reservation.Status = "Expired";
            reservation.ReleasedAtUtc = now;
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return Conflict(new WalletReservationResult { Succeeded = false, ReservationId = reservation.ReservationId, Status = reservation.Status, ErrorMessage = "انتهت صلاحية حجز الرصيد." });
        }

        var wallet = await db.GameWallets.FirstAsync(x => x.GuildId == reservation.GuildId && x.UserDiscordId == reservation.DiscordUserId, ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"GameWallets\" SET \"UpdatedAt\" = \"UpdatedAt\" WHERE \"Id\" = {wallet.Id}", ct);
        if (wallet.Balance < reservation.Amount)
            return Conflict(new WalletReservationResult { Succeeded = false, ReservationId = reservation.ReservationId, Status = reservation.Status, ErrorMessage = "الرصيد غير كافٍ عند تأكيد الحجز." });
        wallet.Balance -= (int)reservation.Amount;
        reservation.Status = "Committed";
        reservation.CommittedAtUtc = now;
        if (!await db.GameWalletTransactions.AnyAsync(x => x.ReferenceId == reservation.Id && x.Type == "WalletReservationCommit", ct))
        {
            db.GameWalletTransactions.Add(new GameWalletTransaction { GuildId = reservation.GuildId, UserDiscordId = reservation.DiscordUserId, Amount = -(int)reservation.Amount, Type = "WalletReservationCommit", Reason = "تأكيد حجز رصيد لعبة", ReferenceId = reservation.Id });
        }
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Ok(new WalletReservationResult { Succeeded = true, ReservationId = reservation.ReservationId, Status = reservation.Status });
    }

    [HttpPost("wallet/reservations/{reservationId}/release")]
    public async Task<IActionResult> ReleaseWallet(string reservationId, CancellationToken ct)
    {
        if (!Authorized()) return Unauthorized(new { message = "Invalid activities service key." });
        var reservation = await db.WalletReservations.FirstOrDefaultAsync(x => x.ReservationId == reservationId, ct);
        if (reservation is null) return NotFound(new WalletReservationResult { Succeeded = false, ErrorMessage = "حجز الرصيد غير موجود." });
        if (reservation.Status == "Committed")
            return Conflict(new WalletReservationResult { Succeeded = false, ReservationId = reservation.ReservationId, Status = reservation.Status, ErrorMessage = "لا يمكن إلغاء حجز تم تأكيده." });
        if (reservation.Status is "Released" or "Expired")
            return Ok(new WalletReservationResult { Succeeded = true, ReservationId = reservation.ReservationId, Status = reservation.Status });
        reservation.Status = "Released";
        reservation.ReleasedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(new WalletReservationResult { Succeeded = true, ReservationId = reservation.ReservationId, Status = reservation.Status });
    }

    [HttpPost("wallet/credits")]
    public async Task<IActionResult> CreditWallet(WalletCreditRequest request, CancellationToken ct)
    {
        if (!Authorized()) return Unauthorized(new { message = "Invalid activities service key." });
        if (!ValidSnowflake(request.DiscordGuildId) || !ValidSnowflake(request.DiscordUserId))
            return BadRequest(new WalletCreditResult { Succeeded = false, Status = "Rejected", ErrorMessage = "بيانات Discord غير صالحة." });
        if (request.Amount <= 0)
            return BadRequest(new WalletCreditResult { Succeeded = false, Status = "Rejected", ErrorMessage = "مبلغ المكافأة يجب أن يكون أكبر من صفر." });
        if (request.Amount != decimal.Truncate(request.Amount))
            return BadRequest(new WalletCreditResult { Succeeded = false, Status = "Rejected", ErrorMessage = "المحفظة الحالية تدعم عملات صحيحة فقط." });
        if (request.PayoutId == Guid.Empty || string.IsNullOrWhiteSpace(request.IdempotencyKey))
            return BadRequest(new WalletCreditResult { Succeeded = false, Status = "Rejected", ErrorMessage = "معرّف المكافأة ومفتاح التكرار مطلوبان." });

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var guildId = await db.Guilds.Where(x => x.DiscordGuildId == request.DiscordGuildId && x.IsActive).Select(x => (Guid?)x.Id).FirstOrDefaultAsync(ct);
        if (!guildId.HasValue)
            return NotFound(new WalletCreditResult { Succeeded = false, Status = "Rejected", ErrorMessage = "هذا السيرفر غير مربوط بمنصة البوت." });

        var existing = await db.GameWalletTransactions.FirstOrDefaultAsync(x => x.ReferenceId == request.PayoutId && x.UserDiscordId == request.DiscordUserId && x.Type == "WalletCredit", ct);
        if (existing is not null)
        {
            await transaction.CommitAsync(ct);
            return Ok(new WalletCreditResult { Succeeded = true, Status = "Credited", Amount = existing.Amount });
        }

        var wallet = await db.GameWallets.FirstOrDefaultAsync(x => x.GuildId == guildId && x.UserDiscordId == request.DiscordUserId, ct);
        if (wallet is null)
        {
            wallet = new GameWallet { GuildId = guildId.Value, UserDiscordId = request.DiscordUserId };
            db.GameWallets.Add(wallet);
            await db.SaveChangesAsync(ct);
        }

        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"GameWallets\" SET \"UpdatedAt\" = \"UpdatedAt\" WHERE \"Id\" = {wallet.Id}", ct);
        wallet.Balance += (int)request.Amount;
        db.GameWalletTransactions.Add(new GameWalletTransaction
        {
            GuildId = guildId.Value,
            UserDiscordId = request.DiscordUserId,
            Amount = (int)request.Amount,
            Type = "WalletCredit",
            Reason = Limit($"{request.Reason}:{request.GameKey}:{request.GameSessionId}:{request.RoundId}:{request.IdempotencyKey}", 500),
            ReferenceId = request.PayoutId
        });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        logger.LogInformation("Activities wallet credit applied for guild {DiscordGuildId}, user {DiscordUserId}, payout {PayoutId}, amount {Amount}.", request.DiscordGuildId, request.DiscordUserId, request.PayoutId, request.Amount);
        return Ok(new WalletCreditResult { Succeeded = true, Status = "Credited", Amount = request.Amount });
    }

    private bool Authorized()
    {
        var expected = configuration["ActivitiesIntegration:ServiceToken"];
        return !string.IsNullOrWhiteSpace(expected)
            && Request.Headers.TryGetValue("X-Activities-Service-Key", out var provided)
            && string.Equals(provided.ToString(), expected, StringComparison.Ordinal);
    }

    private static bool? ManifestBool(string? json, string property)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty(property, out var node) && node.ValueKind is JsonValueKind.True or JsonValueKind.False ? node.GetBoolean() : null;
        }
        catch { return null; }
    }

    private async Task<RouletteSettingsSnapshot?> LoadRouletteSettingsAsync(string discordGuildId, CancellationToken ct)
    {
        var settings = await db.Guilds.AsNoTracking()
            .Where(x => x.DiscordGuildId == discordGuildId && x.IsActive)
            .Select(x => x.RouletteSettings == null ? null : new RouletteSettingsSnapshot
            {
                MinPlayers = x.RouletteSettings.MinPlayers,
                MaxPlayers = x.RouletteSettings.MaxPlayers,
                WinnerCoins = x.RouletteSettings.WinnerCoins,
                SecondPlaceCoins = x.RouletteSettings.SecondPlaceCoins,
                ParticipationCoins = x.RouletteSettings.ParticipationCoins,
                JoinWindowSeconds = x.RouletteSettings.JoinWindowSeconds,
                TurnSeconds = x.RouletteSettings.TurnSeconds
            })
            .FirstOrDefaultAsync(ct);

        return settings ?? new RouletteSettingsSnapshot();
    }

    private async Task ExpireWalletReservationsAsync(DateTimeOffset now, CancellationToken ct)
    {
        var expired = await db.WalletReservations.Where(x => x.Status == "Pending" && x.ExpiresAtUtc <= now).ToListAsync(ct);
        foreach (var item in expired)
        {
            item.Status = "Expired";
            item.ReleasedAtUtc = now;
        }
    }

    public sealed class ValidateActivityGameAccessRequest
    {
        public string DiscordGuildId { get; set; } = string.Empty;
        public string DiscordChannelId { get; set; } = string.Empty;
        public string DiscordUserId { get; set; } = string.Empty;
        public string GameKey { get; set; } = string.Empty;
    }

    public sealed class GameAccessResult
    {
        public bool Allowed { get; set; }
        public string? DenialReason { get; set; }
        public string GameKey { get; set; } = string.Empty;
        public string GameVersion { get; set; } = string.Empty;
        public Guid? PlatformGameVersionId { get; set; }
        public string Mode { get; set; } = "Production";
        public string? ActivityRoute { get; set; }
        public bool SupportsWallet { get; set; }
        public RouletteSettingsSnapshot? RouletteSettings { get; set; }
    }

    public sealed class RouletteSettingsSnapshot
    {
        public int MinPlayers { get; set; } = 2;
        public int MaxPlayers { get; set; } = 6;
        public int WinnerCoins { get; set; } = 100;
        public int SecondPlaceCoins { get; set; } = 50;
        public int ParticipationCoins { get; set; } = 10;
        public int JoinWindowSeconds { get; set; } = 120;
        public int TurnSeconds { get; set; } = 30;
    }

    public sealed class ReserveWalletRequest
    {
        public string DiscordGuildId { get; set; } = string.Empty;
        public string DiscordUserId { get; set; } = string.Empty;
        public string GameKey { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "coins";
        public string IdempotencyKey { get; set; } = string.Empty;
    }

    public sealed class WalletReservationResult
    {
        public bool Succeeded { get; set; }
        public string? ReservationId { get; set; }
        public string? Status { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public sealed class WalletCreditRequest
    {
        public string DiscordGuildId { get; set; } = string.Empty;
        public string DiscordUserId { get; set; } = string.Empty;
        public string GameKey { get; set; } = string.Empty;
        public Guid GameSessionId { get; set; }
        public Guid RoundId { get; set; }
        public Guid PayoutId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "coins";
        public string Reason { get; set; } = "roulette_payout";
        public string IdempotencyKey { get; set; } = string.Empty;
    }

    public sealed class WalletCreditResult
    {
        public bool Succeeded { get; set; }
        public string? Status { get; set; }
        public decimal Amount { get; set; }
        public string? ErrorMessage { get; set; }
    }

    private static bool ValidSnowflake(string value) => ulong.TryParse(value, out _);
    private static string Limit(string? value, int max)
    {
        var clean = string.IsNullOrWhiteSpace(value) ? "" : value.Trim();
        return clean[..Math.Min(clean.Length, max)];
    }
}
