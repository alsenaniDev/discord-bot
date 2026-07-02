using DiscordBot.Domain.Entities;
using DiscordBot.Infrastructure.Data;
using DiscordBot.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace DiscordBot.Infrastructure.Services;

public interface IGuildProfileService
{
    Task<GuildProfileDto?> GetProfileAsync(
        Guid guildId,
        string discordUserId,
        CancellationToken cancellationToken = default);

    Task<GuildProfileDto?> UpdateProfileAsync(
        Guid guildId,
        string discordUserId,
        UpdateGuildProfileRequest request,
        CancellationToken cancellationToken = default);

    Task<GuildProfileDto?> GetProfileByDiscordGuildIdAsync(
        string discordGuildId,
        CancellationToken cancellationToken = default);
}

public class GuildProfileService : IGuildProfileService
{
    private readonly AppDbContext _dbContext;
    private readonly IGuildAccessService _guildAccessService;

    public GuildProfileService(AppDbContext dbContext, IGuildAccessService guildAccessService)
    {
        _dbContext = dbContext;
        _guildAccessService = guildAccessService;
    }

    public async Task<GuildProfileDto?> GetProfileAsync(
        Guid guildId,
        string discordUserId,
        CancellationToken cancellationToken = default)
    {
        var access = await _guildAccessService.GetAccessAsync(guildId, discordUserId, cancellationToken);
        if (access is null || !access.CanManageSettings)
        {
            return null;
        }

        return await LoadProfileAsync(guildId, cancellationToken);
    }

    public async Task<GuildProfileDto?> UpdateProfileAsync(
        Guid guildId,
        string discordUserId,
        UpdateGuildProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var access = await _guildAccessService.GetAccessAsync(guildId, discordUserId, cancellationToken);
        if (access is null || !access.CanManageSettings)
        {
            return null;
        }

        var guild = await _dbContext.Guilds
            .FirstOrDefaultAsync(g => g.Id == guildId && g.IsActive, cancellationToken);

        if (guild is null)
        {
            return null;
        }

        guild.DisplayName = TrimOrNull(request.DisplayName, 256);
        guild.Description = TrimOrNull(request.Description, 2000);
        guild.CommunityType = TrimOrNull(request.CommunityType, 100);
        guild.SupportMessage = TrimOrNull(request.SupportMessage, 2000);
        guild.RulesUrl = ValidateUrlOrNull(request.RulesUrl);
        guild.WebsiteUrl = ValidateUrlOrNull(request.WebsiteUrl);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Map(guild);
    }

    public async Task<GuildProfileDto?> GetProfileByDiscordGuildIdAsync(
        string discordGuildId,
        CancellationToken cancellationToken = default)
    {
        var guildId = await _dbContext.Guilds
            .AsNoTracking()
            .Where(g => g.DiscordGuildId == discordGuildId && g.IsActive)
            .Select(g => g.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (guildId == Guid.Empty)
        {
            return null;
        }

        return await LoadProfileAsync(guildId, cancellationToken);
    }

    private async Task<GuildProfileDto?> LoadProfileAsync(Guid guildId, CancellationToken cancellationToken)
    {
        var guild = await _dbContext.Guilds
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == guildId && g.IsActive, cancellationToken);

        return guild is null ? null : Map(guild);
    }

    private static GuildProfileDto Map(Guild guild) =>
        new()
        {
            GuildId = guild.Id,
            DiscordGuildId = guild.DiscordGuildId,
            Name = guild.Name,
            IconUrl = guild.IconUrl,
            DisplayName = guild.DisplayName,
            Description = guild.Description,
            CommunityType = guild.CommunityType,
            SupportMessage = guild.SupportMessage,
            RulesUrl = guild.RulesUrl,
            WebsiteUrl = guild.WebsiteUrl
        };

    private static string? TrimOrNull(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }

    private static string? ValidateUrlOrNull(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException("URL must start with http:// or https://.");
        }

        return trimmed.Length > 512 ? trimmed[..512] : trimmed;
    }
}
