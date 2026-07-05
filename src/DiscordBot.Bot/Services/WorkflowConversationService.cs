using System.Collections.Concurrent;
using Discord;
using Discord.WebSocket;
using DiscordBot.Bot.Api;
using DiscordBot.Bot.Api.Models;
using DiscordBot.Bot.UI;
using Microsoft.Extensions.Logging;

namespace DiscordBot.Bot.Services;

public class WorkflowConversationService
{
    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(15);
    private readonly ConcurrentDictionary<ulong, Conversation> _active = new();
    private readonly BotApiClient _api;
    private readonly EmbedBuilderService _embeds;
    private readonly ILogger<WorkflowConversationService> _logger;
    public WorkflowConversationService(BotApiClient api, EmbedBuilderService embeds, ILogger<WorkflowConversationService> logger) { _api = api; _embeds = embeds; _logger = logger; }

    public async Task StartFromPanelAsync(SocketMessageComponent component, Guid workflowId)
    {
        var member = component.User as SocketGuildUser;
        if (member is null) { await InteractionResponseHelper.RespondErrorAsync(component, _embeds, "Workflow unavailable", "This workflow must be started inside a server."); return; }
        var context = await _api.GetWorkflowStartContextAsync(workflowId, member.Guild.Id.ToString(), member.Id.ToString());
        if (context is null) { await InteractionResponseHelper.RespondErrorAsync(component, _embeds, "Workflow unavailable", "The workflow could not be loaded."); return; }
        if (!context.CanStart) { await InteractionResponseHelper.RespondInfoAsync(component, _embeds, "Cannot start workflow", context.BlockReason ?? "This workflow cannot be started right now."); return; }
        if (_active.ContainsKey(member.Id)) { await InteractionResponseHelper.RespondInfoAsync(component, _embeds, "Workflow already active", "Finish or cancel your active DM workflow first."); return; }
        if (context.RequireConfirmation)
        {
            var controls = new ComponentBuilder()
                .WithButton(context.ConfirmButtonText, DiscordCustomIds.WorkflowConfirm(workflowId, member.Guild.Id), ButtonStyle.Success)
                .WithButton(context.CancelButtonText, DiscordCustomIds.WorkflowCancel(workflowId, member.Guild.Id), ButtonStyle.Secondary).Build();
            await InteractionResponseHelper.RespondInfoAsync(component, _embeds, context.ConfirmationTitle ?? context.Name, context.ConfirmationMessage ?? "Continue in a private DM?", components: controls);
            _logger.LogInformation("Workflow {WorkflowId} confirmation shown to user {UserId} in guild {GuildId}.", workflowId, member.Id, member.Guild.Id);
            return;
        }
        await BeginAsync(component, context, member.Guild.Id);
    }

    public async Task HandleControlAsync(SocketMessageComponent component, bool confirm, Guid workflowId, ulong guildId)
    {
        if (!confirm) { _logger.LogInformation("Workflow {WorkflowId} was cancelled at confirmation by user {UserId} in guild {GuildId}.", workflowId, component.User.Id, guildId); await component.UpdateAsync(x => { x.Content = WorkflowBotMessages.Cancelled; x.Embed = null; x.Components = null; }); return; }
        var context = await _api.GetWorkflowStartContextAsync(workflowId, guildId.ToString(), component.User.Id.ToString());
        if (context is null || !context.CanStart) { await component.UpdateAsync(x => { x.Content = context?.BlockReason ?? "Workflow unavailable."; x.Embed = null; x.Components = null; }); return; }
        await BeginAsync(component, context, guildId, updateSourceMessage: true);
    }

    public async Task<bool> HandleDmMessageAsync(SocketMessage message)
    {
        if (message.Author.IsBot || message.Channel is not IDMChannel) return false;
        _logger.LogDebug("Workflow DM message received from user {UserId}, message {MessageId}.", message.Author.Id, message.Id);
        if (!_active.TryGetValue(message.Author.Id, out var conversation))
        {
            _logger.LogDebug("Workflow DM from user {UserId} did not match an active conversation.", message.Author.Id);
            return false;
        }
        await conversation.Gate.WaitAsync();
        try
        {
            if (!_active.TryGetValue(message.Author.Id, out var current) || !ReferenceEquals(current, conversation)) return true;
            _logger.LogDebug("Workflow DM matched conversation {ConversationId}, workflow {WorkflowId}, guild {GuildId}, question {QuestionIndex}.", conversation.ConversationId, conversation.WorkflowId, conversation.DiscordGuildId, conversation.Index + 1);
            if (DateTimeOffset.UtcNow - conversation.LastActivity > Timeout) { _active.TryRemove(message.Author.Id, out _); _logger.LogInformation("Workflow conversation {ConversationId} expired for user {UserId}.", conversation.ConversationId, message.Author.Id); await message.Channel.SendMessageAsync(WorkflowBotMessages.Expired); return true; }
            var text = message.Content.Trim();
            if (text.Equals("cancel", StringComparison.OrdinalIgnoreCase)) { _active.TryRemove(message.Author.Id, out _); _logger.LogInformation("Workflow conversation {ConversationId} cancelled by text fallback.", conversation.ConversationId); await message.Channel.SendMessageAsync(WorkflowBotMessages.Cancelled); return true; }
            var question = conversation.Context.Questions[conversation.Index];
            var validation = TryNormalizeAnswer(question, text, out var normalized); if (validation is not null) { _logger.LogDebug("Workflow conversation {ConversationId} rejected answer for question {QuestionId}: {Reason}", conversation.ConversationId, question.Id, validation); await message.Channel.SendMessageAsync(validation, components: BuildQuestionComponents(conversation, question)); return true; }
            await AcceptAnswerAsync(message.Channel, conversation, question, normalized, message.Author.GlobalName ?? message.Author.Username);
            return true;
        }
        finally { conversation.Gate.Release(); }
    }

    public async Task AnswerFromButtonAsync(SocketMessageComponent component, Guid workflowId, string conversationToken, string questionToken, string answerToken)
    {
        if (!_active.TryGetValue(component.User.Id, out var conversation) || conversation.WorkflowId != workflowId || conversation.ConversationToken != conversationToken)
        {
            _logger.LogDebug("Ignored stale workflow answer button for user {UserId}, workflow {WorkflowId}, conversation token {ConversationToken}.", component.User.Id, workflowId, conversationToken);
            await component.RespondAsync(WorkflowBotMessages.NoLongerActive, ephemeral: true);
            return;
        }
        await conversation.Gate.WaitAsync();
        try
        {
            if (!_active.TryGetValue(component.User.Id, out var current) || !ReferenceEquals(current, conversation) || conversation.Index >= conversation.Context.Questions.Count)
            {
                await component.RespondAsync(WorkflowBotMessages.NoLongerActive, ephemeral: true); return;
            }
            if (DateTimeOffset.UtcNow - conversation.LastActivity > Timeout)
            {
                _active.TryRemove(component.User.Id, out _);
                _logger.LogInformation("Workflow conversation {ConversationId} expired for user {UserId}.", conversation.ConversationId, component.User.Id);
                await component.UpdateAsync(x => { x.Content = WorkflowBotMessages.Expired; x.Embed = null; x.Components = null; }); return;
            }
            var question = conversation.Context.Questions[conversation.Index];
            if (QuestionToken(question.Id) != questionToken || !TryResolveButtonAnswer(question, answerToken, out var value, out var display))
            {
                _logger.LogDebug("Ignored stale or invalid workflow answer button for conversation {ConversationId}, question token {QuestionToken}.", conversation.ConversationId, questionToken);
                await component.RespondAsync(WorkflowBotMessages.NoLongerActive, ephemeral: true); return;
            }
            await component.UpdateAsync(x => { x.Content = $"{component.Message.Content}\n**{WorkflowBotMessages.Selected}:** {display}"; x.Components = DisabledQuestionComponents(conversation, question); });
            await AcceptAnswerAsync(component.Channel, conversation, question, value, component.User.GlobalName ?? component.User.Username);
        }
        finally { conversation.Gate.Release(); }
    }

    public async Task CancelFromButtonAsync(SocketMessageComponent component, Guid workflowId, Guid conversationId)
    {
        if (!_active.TryGetValue(component.User.Id, out var conversation)
            || conversation.ConversationId != conversationId || conversation.WorkflowId != workflowId)
        {
            _logger.LogDebug("Ignored stale workflow cancel button {ConversationId} for user {UserId}.", conversationId, component.User.Id);
            await component.RespondAsync(WorkflowBotMessages.NoLongerActive);
            return;
        }
        await conversation.Gate.WaitAsync();
        try
        {
            if (!_active.TryGetValue(component.User.Id, out var current) || !ReferenceEquals(current, conversation)) { await component.RespondAsync(WorkflowBotMessages.NoLongerActive, ephemeral: true); return; }
            _active.TryRemove(component.User.Id, out _);
            _logger.LogInformation("Workflow conversation {ConversationId} cancelled by button for user {UserId}.", conversationId, component.User.Id);
            await component.UpdateAsync(x => { x.Content = WorkflowBotMessages.Cancelled; x.Embed = null; x.Components = null; });
        }
        finally { conversation.Gate.Release(); }
    }

    private async Task BeginAsync(SocketMessageComponent component, WorkflowStartContextApiResponse context, ulong guildId, bool updateSourceMessage = false)
    {
        var conversation = new Conversation(context, guildId, component.User.Id);
        if (!_active.TryAdd(component.User.Id, conversation)) { await InteractionResponseHelper.RespondInfoAsync(component, _embeds, "Workflow already active", "Finish or cancel your active DM workflow first."); return; }
        try
        {
            var dm = await component.User.CreateDMChannelAsync();
            await dm.SendMessageAsync($"**{context.Name}**\n{WorkflowBotMessages.Intro}");
            await AskAsync(dm, conversation);
            _logger.LogInformation("Workflow DM conversation {ConversationId} started for workflow {WorkflowId}, user {UserId}, guild {GuildId}.", conversation.ConversationId, conversation.WorkflowId, conversation.UserDiscordId, conversation.DiscordGuildId);
            if (updateSourceMessage) await component.UpdateAsync(x => { x.Content = WorkflowBotMessages.CheckDm; x.Embed = null; x.Components = null; });
            else await InteractionResponseHelper.RespondSuccessAsync(component, _embeds, "Check your DMs", WorkflowBotMessages.CheckDm);
        }
        catch (Exception ex)
        {
            _active.TryRemove(component.User.Id, out _);
            _logger.LogWarning(ex,
                "Could not start workflow DM for workflow {WorkflowId}, user {UserId}, guild {GuildId}. Active conversation removed.",
                context.WorkflowId,
                component.User.Id,
                guildId);
            // TODO: Support WorkflowStartMode.Modal as a fallback when the member has DMs disabled.
            if (updateSourceMessage) await component.UpdateAsync(x => { x.Content = WorkflowBotMessages.DmClosed; x.Embed = null; x.Components = null; });
            else await InteractionResponseHelper.RespondErrorAsync(component, _embeds, "DM unavailable", WorkflowBotMessages.DmClosed);
        }
    }
    private static Task AskAsync(IMessageChannel dm, Conversation c)
    {
        var q = c.Context.Questions[c.Index]; var help = string.IsNullOrWhiteSpace(q.HelpText) ? "" : $"\n_{q.HelpText}_";
        return dm.SendMessageAsync($"**{c.Index + 1}/{c.Context.Questions.Count} — {q.Label}**{help}", components: BuildQuestionComponents(c, q));
    }
    private static MessageComponent BuildCancelComponents(Conversation c) => new ComponentBuilder()
        .WithButton(WorkflowBotMessages.CancelButton, DiscordCustomIds.WorkflowConversationCancel(c.WorkflowId, c.ConversationId), ButtonStyle.Danger)
        .Build();
    private static MessageComponent BuildQuestionComponents(Conversation c, WorkflowQuestionApiResponse q)
    {
        if (q.Type == "YesNo") return new ComponentBuilder()
            .WithButton(WorkflowBotMessages.YesButton, DiscordCustomIds.WorkflowQuestionAnswer(c.WorkflowId, c.ConversationToken, QuestionToken(q.Id), "yes"), ButtonStyle.Success, row: 0)
            .WithButton(WorkflowBotMessages.NoButton, DiscordCustomIds.WorkflowQuestionAnswer(c.WorkflowId, c.ConversationToken, QuestionToken(q.Id), "no"), ButtonStyle.Danger, row: 0)
            .WithButton(WorkflowBotMessages.CancelButton, DiscordCustomIds.WorkflowConversationCancel(c.WorkflowId, c.ConversationId), ButtonStyle.Secondary, row: 1).Build();
        if (q.Type == "SingleChoice")
        {
            var builder = new ComponentBuilder();
            foreach (var item in q.Options.OrderBy(x => x.SortOrder).Select((option, index) => (option, index)))
                builder.WithButton(item.option.Label[..Math.Min(item.option.Label.Length, 80)], DiscordCustomIds.WorkflowQuestionAnswer(c.WorkflowId, c.ConversationToken, QuestionToken(q.Id), $"option-{item.index}"), ButtonStyle.Primary, row: 0);
            return builder.WithButton(WorkflowBotMessages.CancelButton, DiscordCustomIds.WorkflowConversationCancel(c.WorkflowId, c.ConversationId), ButtonStyle.Secondary, row: 1).Build();
        }
        return BuildCancelComponents(c);
    }
    private static MessageComponent DisabledQuestionComponents(Conversation c, WorkflowQuestionApiResponse q)
    {
        if (q.Type == "YesNo") return new ComponentBuilder().WithButton(WorkflowBotMessages.YesButton, "answered:yes", ButtonStyle.Success, disabled: true).WithButton(WorkflowBotMessages.NoButton, "answered:no", ButtonStyle.Danger, disabled: true).Build();
        var builder = new ComponentBuilder();
        foreach (var item in q.Options.OrderBy(x => x.SortOrder).Select((option, index) => (option, index))) builder.WithButton(item.option.Label[..Math.Min(item.option.Label.Length, 80)], $"answered:{item.index}", ButtonStyle.Primary, disabled: true);
        return builder.Build();
    }
    private async Task AcceptAnswerAsync(IMessageChannel channel, Conversation conversation, WorkflowQuestionApiResponse question, string value, string displayName)
    {
        conversation.Answers.Add(new WorkflowAnswerApiRequest { QuestionId = question.Id, Label = question.Label, Value = value });
        _logger.LogDebug("Workflow conversation {ConversationId} accepted answer for question {QuestionId}.", conversation.ConversationId, question.Id);
        conversation.Index++; conversation.LastActivity = DateTimeOffset.UtcNow;
        if (conversation.Index < conversation.Context.Questions.Count) { await AskAsync(channel, conversation); _logger.LogDebug("Workflow conversation {ConversationId} sent question {QuestionIndex}.", conversation.ConversationId, conversation.Index + 1); return; }
        _active.TryRemove(ulong.Parse(conversation.UserDiscordId), out _);
        _logger.LogInformation("Submitting workflow conversation {ConversationId}, workflow {WorkflowId}, user {UserId}, guild {GuildId}.", conversation.ConversationId, conversation.WorkflowId, conversation.UserDiscordId, conversation.DiscordGuildId);
        var result = await _api.CreateWorkflowSubmissionAsync(conversation.Context.WorkflowId, new CreateWorkflowSubmissionApiRequest
        { DiscordGuildId = conversation.DiscordGuildId, UserDiscordId = conversation.UserDiscordId, UserDisplayName = displayName, Answers = conversation.Answers });
        if (result.Value is not null) _logger.LogInformation("Workflow conversation {ConversationId} completed as submission {SubmissionId}.", conversation.ConversationId, result.Value.SubmissionId);
        else _logger.LogWarning("Workflow conversation {ConversationId} submission failed: {Error}", conversation.ConversationId, result.Error);
        await channel.SendMessageAsync(result.Value?.Message ?? result.Error ?? WorkflowBotMessages.SubmitFailed);
    }
    private static string? TryNormalizeAnswer(WorkflowQuestionApiResponse q, string value, out string normalized)
    {
        normalized = value.Trim();
        if (q.IsRequired && value.Length == 0) return WorkflowBotMessages.Required;
        if (q.Type is "ShortText" or "LongText" && q.MinLength.HasValue && value.Length < q.MinLength) return $"Please enter at least {q.MinLength} characters.";
        if (q.Type is "ShortText" or "LongText" && q.MaxLength.HasValue && value.Length > q.MaxLength) return $"Please use no more than {q.MaxLength} characters.";
        if (q.Type == "Number" && value.Length > 0 && !decimal.TryParse(value, out _)) return WorkflowBotMessages.NumberRequired;
        if (q.Type == "YesNo") { normalized = NormalizeYesNo(value) ?? ""; if (normalized.Length == 0) return WorkflowBotMessages.YesNoRequired; }
        if (q.Type == "SingleChoice")
        {
            var option = q.Options.FirstOrDefault(x => x.Value.Equals(value, StringComparison.OrdinalIgnoreCase) || x.Label.Equals(value, StringComparison.OrdinalIgnoreCase));
            if (option is null) return WorkflowBotMessages.ChoiceRequired;
            normalized = option.Value;
        }
        return null;
    }
    private static string? NormalizeYesNo(string value) => value.Trim().ToLowerInvariant() switch { "yes" or "y" or "true" or "نعم" or "ايه" or "أيه" or "ايوه" or "أيوه" => "yes", "no" or "n" or "false" or "لا" => "no", _ => null };
    private static bool TryResolveButtonAnswer(WorkflowQuestionApiResponse q, string token, out string value, out string display)
    {
        value = display = "";
        if (q.Type == "YesNo" && token is "yes" or "no") { value = token; display = token == "yes" ? WorkflowBotMessages.YesButton : WorkflowBotMessages.NoButton; return true; }
        if (q.Type != "SingleChoice" || !token.StartsWith("option-", StringComparison.Ordinal) || !int.TryParse(token[7..], out var index)) return false;
        var options = q.Options.OrderBy(x => x.SortOrder).ToList(); if (index < 0 || index >= options.Count) return false;
        value = options[index].Value; display = options[index].Label; return true;
    }
    private static string QuestionToken(Guid id) => id.ToString("N")[..8];
    // TODO: Persist active conversations so they can resume after a bot restart.
    private sealed class Conversation(WorkflowStartContextApiResponse context, ulong guildId, ulong userId)
    {
        public Guid ConversationId { get; } = Guid.NewGuid(); public WorkflowStartContextApiResponse Context { get; } = context;
        public string ConversationToken => ConversationId.ToString("N")[..8]; public SemaphoreSlim Gate { get; } = new(1, 1);
        public Guid WorkflowId => Context.WorkflowId; public string DiscordGuildId { get; } = guildId.ToString(); public string UserDiscordId { get; } = userId.ToString();
        public int Index { get; set; } public DateTimeOffset LastActivity { get; set; } = DateTimeOffset.UtcNow; public List<WorkflowAnswerApiRequest> Answers { get; } = [];
    }
}

internal static class WorkflowBotMessages
{
    public const string Intro = "Answer each question in this DM. Use the Cancel button to stop; typing `cancel` also works as a fallback.";
    public const string CheckDm = "I sent you a private message to continue the workflow.";
    public const string DmClosed = "I could not send you a DM. Open this server's Privacy Settings, enable Direct Messages, then try again. No application was started.";
    public const string Cancelled = "The workflow was cancelled.";
    public const string Expired = "This workflow expired after 15 minutes of inactivity. Start it again from the server.";
    public const string Required = "This question requires an answer.";
    public const string NumberRequired = "Please enter a valid number.";
    public const string YesNoRequired = "Please answer نعم / Yes or لا / No.";
    public const string ChoiceRequired = "Please select one of the available options.";
    public const string YesButton = "نعم / Yes";
    public const string NoButton = "لا / No";
    public const string Selected = "Selected";
    public const string SubmitFailed = "Your submission could not be saved. Please try again.";
    public const string CancelButton = "Cancel application";
    public const string NoLongerActive = "This workflow conversation is no longer active.";
}
