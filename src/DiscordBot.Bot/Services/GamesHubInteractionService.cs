using Discord;
using Discord.WebSocket;
using DiscordBot.Bot.Api;
using DiscordBot.Bot.Api.Models;
using Microsoft.Extensions.Logging;

namespace DiscordBot.Bot.Services;

public class GamesHubInteractionService(BotApiClient api, DiscordActivityLaunchService activityLauncher, ILogger<GamesHubInteractionService> logger)
{
    public const string ComponentPrefix = "games:";

    public async Task ShowHubAsync(SocketInteraction interaction)
    {
        if (interaction.GuildId is not { } guildId) { await ReplyAsync(interaction, "هذا الأمر يعمل داخل السيرفرات فقط."); return; }
        using var validationTimeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(1400));
        var context = await api.GetGamesContextAsync(guildId.ToString(), validationTimeout.Token);
        if (context is null) { await ReplyAsync(interaction, "تعذر تحميل مركز الألعاب الآن. حاول مرة ثانية بعد قليل."); return; }
        if (!context.GuildLinked) { await ReplyAsync(interaction, "هذا السيرفر غير مربوط بمنصة البوت."); return; }
        if (!context.IsEnabled) { await ReplyAsync(interaction, "الألعاب غير مفعّلة في هذا السيرفر."); return; }
        if (string.IsNullOrWhiteSpace(context.GamesChannelDiscordId)) { await ReplyAsync(interaction, "لم يتم تحديد روم الألعاب بعد."); return; }
        if (interaction.Channel.Id.ToString() != context.GamesChannelDiscordId) { await ReplyAsync(interaction, $"🎮 الألعاب متاحة فقط في روم <#{context.GamesChannelDiscordId}>."); return; }
        if (await activityLauncher.TryLaunchAsync(interaction)) return;
        logger.LogWarning("Falling back to the button Games Hub for guild {GuildId}, channel {ChannelId}.", guildId, interaction.Channel.Id);
        var components = new ComponentBuilder();
        foreach (var game in context.Games.Take(20)) components.WithButton(game.Name, $"{ComponentPrefix}play:{game.Key}", ButtonStyle.Primary, new Emoji("🎮"));
        components.WithButton("الترتيب", $"{ComponentPrefix}leaderboard", ButtonStyle.Secondary, new Emoji("🏆"));
        var embed = new EmbedBuilder().WithTitle("🎮 مركز الألعاب").WithDescription(context.Games.Count == 0 ? "تعذر فتح واجهة الألعاب الآن، ولا توجد ألعاب مفعّلة للاستخدام البديل." : "تعذر فتح واجهة الألعاب الآن. تقدر تستخدم الأزرار مؤقتًا أو تحاول مرة أخرى لاحقًا.").WithColor(new Color(88, 101, 242)).Build();
        await interaction.RespondAsync(embed: embed, components: components.Build(), ephemeral: true);
    }

    public async Task HandleButtonAsync(SocketMessageComponent component)
    {
        if (component.GuildId is not { } guildId) { await ReplyAsync(component, "هذا الزر يعمل داخل السيرفرات فقط."); return; }
        var action = component.Data.CustomId[ComponentPrefix.Length..];
        if (action == "leaderboard") { await ShowLeaderboardAsync(component, guildId); return; }
        if (!action.StartsWith("play:", StringComparison.Ordinal)) { await ReplyAsync(component, "هذا الزر غير صالح."); return; }
        var gameKey = action[5..];
        var result = await api.StartGameSessionAsync(new StartGameSessionApiRequest { GuildDiscordId = guildId.ToString(), ChannelDiscordId = component.Channel.Id.ToString(), UserDiscordId = component.User.Id.ToString(), Username = component.User.GlobalName ?? component.User.Username, GameKey = gameKey });
        if (result.Value is null) { await ReplyAsync(component, result.Error ?? "تعذر بدء اللعبة الآن."); return; }
        logger.LogInformation("Game session {SessionId} started from Discord for game {GameKey}, guild {GuildId}, user {UserId}.", result.Value.SessionId, result.Value.GameKey, guildId, component.User.Id);
        await component.RespondAsync($"بدأت جلسة لعبة **{result.Value.GameName}**. سيتم ربط واجهة الألعاب لاحقًا.", ephemeral: true);
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
