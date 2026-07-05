using Discord.WebSocket;
using DiscordBot.Bot.Api;
using DiscordBot.Bot.Api.Models;
using Microsoft.Extensions.Logging;

namespace DiscordBot.Bot.Services;

public class WorkflowActionSyncService
{
    private readonly BotApiClient _api; private readonly ILogger<WorkflowActionSyncService> _logger;
    public WorkflowActionSyncService(BotApiClient api, ILogger<WorkflowActionSyncService> logger) { _api = api; _logger = logger; }
    public async Task ProcessAsync(DiscordSocketClient client, CancellationToken ct)
    {
        var actions = await _api.GetPendingWorkflowActionsAsync(ct);
        foreach (var action in actions)
        {
            try { await ExecuteAsync(client, action); await _api.AckWorkflowActionAsync(action.Id, new AckWorkflowPendingActionApiRequest { Success = true }, ct); }
            catch (Exception ex) { _logger.LogError(ex, "Workflow action {ActionId} for submission {SubmissionId} failed.", action.Id, action.SubmissionId); await _api.AckWorkflowActionAsync(action.Id, new AckWorkflowPendingActionApiRequest { Success = false, FailureReason = ex.Message }, ct); }
        }
    }
    private static async Task ExecuteAsync(DiscordSocketClient client, WorkflowPendingActionApiResponse action)
    {
        if (!ulong.TryParse(action.DiscordGuildId, out var guildId) || !ulong.TryParse(action.UserDiscordId, out var userId)) throw new InvalidOperationException("Invalid workflow guild or user ID.");
        var guild = client.GetGuild(guildId) ?? throw new InvalidOperationException("Discord guild not found.");
        if (action.ActionType == "SendDirectMessage")
        {
            Discord.IUser? user = client.GetUser(userId) ?? (Discord.IUser?)await client.Rest.GetUserAsync(userId);
            if (user is null) throw new InvalidOperationException("Discord user not found.");
            var dm = await user.CreateDMChannelAsync(); await dm.SendMessageAsync(action.MessageText ?? throw new InvalidOperationException("DM text is missing.")); return;
        }
        var member = guild.GetUser(userId) ?? throw new InvalidOperationException("Member is no longer in the guild.");
        if (!ulong.TryParse(action.RoleDiscordId, out var roleId)) throw new InvalidOperationException("Role ID is invalid.");
        var role = guild.GetRole(roleId) ?? throw new InvalidOperationException("Role not found.");
        if (role.IsManaged || !guild.CurrentUser.GuildPermissions.ManageRoles || role.Position >= guild.CurrentUser.Hierarchy) throw new InvalidOperationException("Bot cannot manage the configured role due to permissions or hierarchy.");
        if (action.ActionType == "AddRole") await member.AddRoleAsync(role);
        else if (action.ActionType == "RemoveRole") await member.RemoveRoleAsync(role);
        else throw new InvalidOperationException("Unsupported workflow action.");
    }
}
