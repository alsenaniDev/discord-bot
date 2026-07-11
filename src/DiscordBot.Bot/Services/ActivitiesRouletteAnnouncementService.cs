using Discord;
using Discord.WebSocket;
using DiscordBot.Bot.Api;
using DiscordBot.Bot.Api.Models;

namespace DiscordBot.Bot.Services;

public class ActivitiesRouletteAnnouncementService(ActivitiesApiClient api, ILogger<ActivitiesRouletteAnnouncementService> logger)
{
    public async Task ProcessAsync(DiscordSocketClient client, CancellationToken ct)
    {
        var announcements = await api.GetPendingRouletteAnnouncementsAsync(ct);
        foreach (var announcement in announcements)
        {
            try
            {
                if (!ulong.TryParse(announcement.DiscordGuildId, out var guildId) || !ulong.TryParse(announcement.DiscordChannelId, out var channelId))
                    throw new InvalidOperationException("معرّف السيرفر أو الروم غير صالح.");

                var guild = client.GetGuild(guildId) ?? throw new InvalidOperationException("السيرفر غير موجود لدى البوت.");
                if (guild.GetChannel(channelId) is not IMessageChannel channel)
                    throw new InvalidOperationException("روم الألعاب غير موجود أو ليس رومًا نصيًا.");

                if (guild.GetChannel(channelId) is SocketGuildChannel guildChannel)
                {
                    var permissions = guild.CurrentUser.GetPermissions(guildChannel);
                    if (!permissions.ViewChannel || !permissions.SendMessages)
                        throw new InvalidOperationException("البوت لا يملك صلاحية عرض الروم أو إرسال الرسائل فيه.");
                }

                var embed = new EmbedBuilder()
                    .WithTitle("🎰 تم إنشاء غرفة روليت جديدة")
                    .WithDescription($"أنشأ **{announcement.CreatorUsername}** غرفة جديدة.\nاضغط الزر للانضمام واللعب مع الأعضاء.")
                    .AddField("اللعبة", "الروليت", true)
                    .AddField("اللاعبون", $"{announcement.PlayersCount} / {announcement.MaxPlayers}", true)
                    .AddField("الحد الأدنى", announcement.MinPlayers, true)
                    .AddField("المكافأة", $"{announcement.WinnerCoins} عملة", true)
                    .AddField("الحالة", ArabicStatus(announcement.Status), true)
                    .AddField("مدة الانضمام", $"{Math.Max(0, announcement.JoinWindowSeconds)} ثانية", true)
                    .WithColor(new Color(235, 69, 158))
                    .Build();

                var components = new ComponentBuilder()
                    .WithButton("ادخل التحدي", $"games:activities-roulette:join:{announcement.GameSessionId:D}", ButtonStyle.Primary, new Emoji("🎡"))
                    .Build();

                var message = await channel.SendMessageAsync(embed: embed, components: components, allowedMentions: AllowedMentions.None);
                await api.AckRouletteAnnouncementAsync(announcement.GameSessionId, new AckActivitiesRouletteAnnouncementApiRequest { Success = true, MessageDiscordId = message.Id.ToString() }, ct);
                logger.LogInformation(
                    "Published Activities Roulette announcement. GameSessionId={GameSessionId}, GuildId={GuildId}, ChannelId={ChannelId}, DiscordMessageId={DiscordMessageId}.",
                    announcement.GameSessionId,
                    announcement.DiscordGuildId,
                    announcement.DiscordChannelId,
                    message.Id);
            }
            catch (Discord.Net.HttpException ex) when (ex.HttpCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                logger.LogWarning(ex, "Discord rate limited Activities Roulette announcement. GameSessionId={GameSessionId}, GuildId={GuildId}, ChannelId={ChannelId}.", announcement.GameSessionId, announcement.DiscordGuildId, announcement.DiscordChannelId);
                await api.AckRouletteAnnouncementAsync(announcement.GameSessionId, new AckActivitiesRouletteAnnouncementApiRequest { Success = false, ErrorMessage = "Discord يقيّد إرسال رسائل الروليت مؤقتًا.", RetryAfterSeconds = 300 }, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Activities Roulette announcement failed. GameSessionId={GameSessionId}, GuildId={GuildId}, ChannelId={ChannelId}, ExceptionType={ExceptionType}.", announcement.GameSessionId, announcement.DiscordGuildId, announcement.DiscordChannelId, ex.GetType().Name);
                await api.AckRouletteAnnouncementAsync(announcement.GameSessionId, new AckActivitiesRouletteAnnouncementApiRequest { Success = false, ErrorMessage = ex.Message }, ct);
            }
        }
    }

    private static string ArabicStatus(string status) => status switch
    {
        "Waiting" => "بانتظار اللاعبين",
        "InProgress" => "بدأت اللعبة",
        "Completed" => "مكتملة",
        "Cancelled" => "ملغية",
        "Expired" => "منتهية",
        _ => status
    };
}
