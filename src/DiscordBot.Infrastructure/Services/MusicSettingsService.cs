using DiscordBot.Domain.Entities;
using DiscordBot.Infrastructure.Data;
using DiscordBot.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace DiscordBot.Infrastructure.Services;

public interface IMusicSettingsService
{
    Task<GuildMusicSettingsDto?> GetAsync(Guid guildId, CancellationToken cancellationToken = default);
    Task<GuildMusicSettingsDto?> GetByDiscordGuildIdAsync(string discordGuildId, CancellationToken cancellationToken = default);
    Task<(GuildMusicSettingsDto? Value, string? Error)> UpdateAsync(Guid guildId, UpdateGuildMusicSettingsRequest request, CancellationToken cancellationToken = default);
}

public class MusicSettingsService(AppDbContext db) : IMusicSettingsService
{
    public async Task<GuildMusicSettingsDto?> GetAsync(Guid guildId, CancellationToken cancellationToken = default)
    {
        if (!await db.Guilds.AsNoTracking().AnyAsync(x => x.Id == guildId, cancellationToken)) return null;
        var settings = await db.GuildMusicSettings.AsNoTracking().FirstOrDefaultAsync(x => x.GuildId == guildId, cancellationToken);
        return Map(guildId, settings);
    }

    public async Task<GuildMusicSettingsDto?> GetByDiscordGuildIdAsync(string discordGuildId, CancellationToken cancellationToken = default)
    {
        var guild = await db.Guilds.AsNoTracking().Where(x => x.DiscordGuildId == discordGuildId)
            .Select(x => new { x.Id, Music = x.MusicSettings }).FirstOrDefaultAsync(cancellationToken);
        return guild is null ? null : Map(guild.Id, guild.Music);
    }

    public async Task<(GuildMusicSettingsDto? Value, string? Error)> UpdateAsync(Guid guildId, UpdateGuildMusicSettingsRequest request, CancellationToken cancellationToken = default)
    {
        var error = Validate(request); if (error is not null) return (null, error);
        if (!await db.Guilds.AnyAsync(x => x.Id == guildId, cancellationToken)) return (null, null);
        var settings = await db.GuildMusicSettings.FirstOrDefaultAsync(x => x.GuildId == guildId, cancellationToken);
        if (settings is null) { settings = new GuildMusicSettings { GuildId = guildId }; db.GuildMusicSettings.Add(settings); }
        settings.IsEnabled = request.IsEnabled;
        settings.DjRoleDiscordId = string.IsNullOrWhiteSpace(request.DjRoleDiscordId) ? null : request.DjRoleDiscordId.Trim();
        settings.MaxQueueSize = request.MaxQueueSize;
        settings.MaxTrackDurationSeconds = request.MaxTrackDurationSeconds;
        settings.DefaultVolume = request.DefaultVolume;
        settings.AllowEveryoneToQueue = request.AllowEveryoneToQueue;
        await db.SaveChangesAsync(cancellationToken);
        return (Map(guildId, settings), null);
    }

    private static string? Validate(UpdateGuildMusicSettingsRequest request)
    {
        if (request.MaxQueueSize is < 1 or > 200) return "Max queue size must be between 1 and 200.";
        if (request.MaxTrackDurationSeconds is < 60 or > 7200) return "Max track duration must be between 60 and 7200 seconds.";
        if (request.DefaultVolume is < 1 or > 100) return "Default volume must be between 1 and 100.";
        if (!string.IsNullOrWhiteSpace(request.DjRoleDiscordId) && !ulong.TryParse(request.DjRoleDiscordId, out _)) return "DJ role must be a valid Discord role ID.";
        return null;
    }

    private static GuildMusicSettingsDto Map(Guid guildId, GuildMusicSettings? x) => new()
    {
        GuildId = guildId, IsEnabled = x?.IsEnabled ?? false, DjRoleDiscordId = x?.DjRoleDiscordId,
        MaxQueueSize = x?.MaxQueueSize ?? 50, MaxTrackDurationSeconds = x?.MaxTrackDurationSeconds ?? 600,
        DefaultVolume = x?.DefaultVolume ?? 50, AllowEveryoneToQueue = x?.AllowEveryoneToQueue ?? true,
        CreatedAtUtc = x?.CreatedAt, UpdatedAtUtc = x?.UpdatedAt
    };
}
