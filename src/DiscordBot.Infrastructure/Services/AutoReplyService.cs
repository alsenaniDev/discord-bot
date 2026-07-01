using DiscordBot.Domain.Entities;
using DiscordBot.Domain.Enums;
using DiscordBot.Infrastructure.Data;
using DiscordBot.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace DiscordBot.Infrastructure.Services;

public interface IAutoReplyService
{
    Task<IReadOnlyList<AutoReplyRuleDto>> GetRulesAsync(
        Guid guildId,
        string discordUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AutoReplyRuleDto>> GetEnabledRulesByDiscordGuildIdAsync(
        string discordGuildId,
        CancellationToken cancellationToken = default);

    Task<AutoReplyRuleDto?> CreateAsync(
        Guid guildId,
        string discordUserId,
        CreateAutoReplyRuleRequest request,
        CancellationToken cancellationToken = default);

    Task<AutoReplyRuleDto?> UpdateAsync(
        Guid guildId,
        Guid ruleId,
        string discordUserId,
        UpdateAutoReplyRuleRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid guildId,
        Guid ruleId,
        string discordUserId,
        CancellationToken cancellationToken = default);
}

public class AutoReplyService : IAutoReplyService
{
    private readonly AppDbContext _dbContext;
    private readonly IGuildAccessService _guildAccessService;

    public AutoReplyService(AppDbContext dbContext, IGuildAccessService guildAccessService)
    {
        _dbContext = dbContext;
        _guildAccessService = guildAccessService;
    }

    public async Task<IReadOnlyList<AutoReplyRuleDto>> GetRulesAsync(
        Guid guildId,
        string discordUserId,
        CancellationToken cancellationToken = default)
    {
        if (!await _guildAccessService.IsOwnerAsync(guildId, discordUserId, cancellationToken))
        {
            return [];
        }

        var rules = await _dbContext.AutoReplyRules
            .AsNoTracking()
            .Where(r => r.GuildId == guildId)
            .OrderBy(r => r.Priority)
            .ThenBy(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        return rules.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<AutoReplyRuleDto>> GetEnabledRulesByDiscordGuildIdAsync(
        string discordGuildId,
        CancellationToken cancellationToken = default)
    {
        var rules = await _dbContext.AutoReplyRules
            .AsNoTracking()
            .Where(r => r.Guild.DiscordGuildId == discordGuildId && r.Guild.IsActive && r.Enabled)
            .OrderBy(r => r.Priority)
            .ThenBy(r => r.CreatedAt)
            .Select(r => new AutoReplyRuleDto
            {
                Id = r.Id,
                GuildId = r.GuildId,
                Trigger = r.Trigger,
                Response = r.Response,
                MatchMode = r.MatchMode,
                Scope = r.Scope,
                Enabled = r.Enabled,
                Priority = r.Priority,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return rules;
    }

    public async Task<AutoReplyRuleDto?> CreateAsync(
        Guid guildId,
        string discordUserId,
        CreateAutoReplyRuleRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await _guildAccessService.IsOwnerAsync(guildId, discordUserId, cancellationToken))
        {
            return null;
        }

        var trigger = request.Trigger.Trim();
        var response = request.Response.Trim();
        if (string.IsNullOrWhiteSpace(trigger) || string.IsNullOrWhiteSpace(response))
        {
            return null;
        }

        var rule = new AutoReplyRule
        {
            GuildId = guildId,
            Trigger = trigger,
            Response = response,
            MatchMode = request.MatchMode,
            Scope = request.Scope,
            Enabled = request.Enabled,
            Priority = request.Priority
        };

        _dbContext.AutoReplyRules.Add(rule);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Map(rule);
    }

    public async Task<AutoReplyRuleDto?> UpdateAsync(
        Guid guildId,
        Guid ruleId,
        string discordUserId,
        UpdateAutoReplyRuleRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await _guildAccessService.IsOwnerAsync(guildId, discordUserId, cancellationToken))
        {
            return null;
        }

        var rule = await _dbContext.AutoReplyRules
            .FirstOrDefaultAsync(r => r.Id == ruleId && r.GuildId == guildId, cancellationToken);

        if (rule is null)
        {
            return null;
        }

        var trigger = request.Trigger.Trim();
        var response = request.Response.Trim();
        if (string.IsNullOrWhiteSpace(trigger) || string.IsNullOrWhiteSpace(response))
        {
            return null;
        }

        rule.Trigger = trigger;
        rule.Response = response;
        rule.MatchMode = request.MatchMode;
        rule.Scope = request.Scope;
        rule.Enabled = request.Enabled;
        rule.Priority = request.Priority;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Map(rule);
    }

    public async Task<bool> DeleteAsync(
        Guid guildId,
        Guid ruleId,
        string discordUserId,
        CancellationToken cancellationToken = default)
    {
        if (!await _guildAccessService.IsOwnerAsync(guildId, discordUserId, cancellationToken))
        {
            return false;
        }

        var rule = await _dbContext.AutoReplyRules
            .FirstOrDefaultAsync(r => r.Id == ruleId && r.GuildId == guildId, cancellationToken);

        if (rule is null)
        {
            return false;
        }

        _dbContext.AutoReplyRules.Remove(rule);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public static bool Matches(string messageContent, AutoReplyRuleDto rule)
    {
        if (string.IsNullOrWhiteSpace(messageContent) || string.IsNullOrWhiteSpace(rule.Trigger))
        {
            return false;
        }

        return rule.MatchMode switch
        {
            AutoReplyMatchMode.Exact => string.Equals(
                messageContent.Trim(),
                rule.Trigger.Trim(),
                StringComparison.OrdinalIgnoreCase),
            _ => messageContent.Contains(rule.Trigger, StringComparison.OrdinalIgnoreCase)
        };
    }

    private static AutoReplyRuleDto Map(AutoReplyRule rule) =>
        new()
        {
            Id = rule.Id,
            GuildId = rule.GuildId,
            Trigger = rule.Trigger,
            Response = rule.Response,
            MatchMode = rule.MatchMode,
            Scope = rule.Scope,
            Enabled = rule.Enabled,
            Priority = rule.Priority,
            CreatedAt = rule.CreatedAt
        };
}
