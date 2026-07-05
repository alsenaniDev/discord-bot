using Discord;
using Discord.WebSocket;
using DiscordBot.Bot.Api;
using DiscordBot.Bot.Api.Models;
using DiscordBot.Bot.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DiscordBot.Bot.Services;

public class GamesHubInteractionService(BotApiClient api, GamesContextCache contextCache, DiscordActivityLaunchService activityLauncher, IOptions<DiscordActivityOptions> activityOptions, ILogger<GamesHubInteractionService> logger)
{
    public const string ComponentPrefix = "games:";

    public async Task ShowHubAsync(SocketInteraction interaction)
    {
        if (interaction.GuildId is not { } guildId) { await ReplyAsync(interaction, "هذا الأمر يعمل داخل السيرفرات فقط."); return; }
        if (!contextCache.TryGet(guildId, out var context))
        {
            _ = contextCache.RefreshAsync(guildId);
            await ReplyAsync(interaction, "جاري تجهيز مركز الألعاب. حاول مرة ثانية بعد لحظات.");
            return;
        }
        if (!context.GuildLinked) { await ReplyAsync(interaction, "هذا السيرفر غير مربوط بمنصة البوت."); return; }
        if (!context.IsEnabled) { await ReplyAsync(interaction, "الألعاب غير مفعّلة في هذا السيرفر."); return; }
        if (string.IsNullOrWhiteSpace(context.GamesChannelDiscordId)) { await ReplyAsync(interaction, "لم يتم تحديد روم الألعاب بعد."); return; }
        if (interaction.Channel.Id.ToString() != context.GamesChannelDiscordId) { await ReplyAsync(interaction, $"🎮 الألعاب متاحة فقط في روم <#{context.GamesChannelDiscordId}>."); return; }
        if (activityOptions.Value.Enabled)
        {
            // An interaction callback is single-use even when Discord rejects type 12.
            // Never attempt a second initial response after sending the launch callback.
            var launch = await activityLauncher.TryLaunchAsync(interaction);
            if (launch.WasAccepted) return;
            if (!launch.CanFallback)
            {
                logger.LogWarning("Discord Activity launch failed after initial response. Cannot fallback.");
                return;
            }
            logger.LogWarning("Discord Activity launch failed before initial response. Showing fallback hub.");
            await SendButtonHubAsync(interaction, context);
            return;
        }
        logger.LogWarning("Discord Activity launch failed before initial response. Showing fallback hub. Activity launch is disabled by configuration.");
        await SendButtonHubAsync(interaction, context);
    }

    private async Task SendButtonHubAsync(SocketInteraction interaction, BotGamesContextApiResponse context)
    {
        var components = new ComponentBuilder();
        foreach (var game in context.Games.Take(20))
        {
            var label = game.Key.Equals("quiz", StringComparison.OrdinalIgnoreCase) ? "لعبة الأسئلة - احتياطي" : $"{game.Name} - احتياطي";
            components.WithButton(label, $"{ComponentPrefix}play:{game.Key}", ButtonStyle.Primary, new Emoji("🎮"));
        }
        components.WithButton("الترتيب", $"{ComponentPrefix}leaderboard", ButtonStyle.Secondary, new Emoji("🏆"));
        var embed = new EmbedBuilder().WithTitle("🎮 مركز الألعاب - الوضع الاحتياطي").WithDescription("تعذر فتح واجهة الألعاب داخل Discord، لذلك تم عرض الوضع الاحتياطي.").WithColor(new Color(88, 101, 242)).Build();
        try { await interaction.RespondAsync(embed: embed, components: components.Build(), ephemeral: true); }
        catch (Exception ex) { logger.LogWarning(ex, "Could not send Games Hub fallback for interaction {InteractionId}.", interaction.Id); }
    }

    public async Task HandleButtonAsync(SocketMessageComponent component)
    {
        if (component.GuildId is not { } guildId) { await ReplyAsync(component, "هذا الزر يعمل داخل السيرفرات فقط."); return; }
        var action = component.Data.CustomId[ComponentPrefix.Length..];
        if (action == "leaderboard") { await ShowLeaderboardAsync(component, guildId); return; }
        if (!action.StartsWith("play:", StringComparison.Ordinal)) { await ReplyAsync(component, "هذا الزر غير صالح."); return; }
        var gameKey = action[5..];
        await component.DeferAsync(ephemeral: true);
        var result = await api.StartGameSessionAsync(new StartGameSessionApiRequest { GuildDiscordId = guildId.ToString(), ChannelDiscordId = component.Channel.Id.ToString(), UserDiscordId = component.User.Id.ToString(), Username = component.User.GlobalName ?? component.User.Username, GameKey = gameKey });
        if (result.Value is null) { await component.ModifyOriginalResponseAsync(x => x.Content = result.Error ?? "تعذر بدء اللعبة الآن."); return; }
        logger.LogInformation("Game session {SessionId} started from Discord for game {GameKey}, guild {GuildId}, user {UserId}.", result.Value.SessionId, result.Value.GameKey, guildId, component.User.Id);
        await component.ModifyOriginalResponseAsync(x => x.Content = $"بدأت جلسة احتياطية للعبة **{result.Value.GameName}**. واجهة Discord Activity لم تُفتح في هذه المحاولة.");
    }

    private async Task ShowLeaderboardAsync(SocketMessageComponent component, ulong guildId)
    {
        var leaders = await api.GetGameLeaderboardAsync(guildId.ToString());
        if (leaders is null) { await ReplyAsync(component, "تعذر تحميل الترتيب الآن."); return; }
        var body = leaders.Count == 0 ? "لا توجد نتائج حتى الآن. كن أول لاعب في الترتيب!" : string.Join('\n', leaders.Select((x, i) => $"{i + 1}. {x.Username} — {x.TotalPoints} نقطة"));
        await component.RespondAsync(embed: new EmbedBuilder().WithTitle("🏆 ترتيب الألعاب").WithDescription(body).WithColor(new Color(241, 196, 15)).Build(), ephemeral: true);
    }

    private static Task ReplyAsync(SocketInteraction interaction, string message) => interaction.RespondAsync(message, ephemeral: true, allowedMentions: AllowedMentions.None);
}
