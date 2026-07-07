using Discord;
using Discord.WebSocket;
using DiscordBot.Bot.Api;
using DiscordBot.Bot.Api.Models;
using Microsoft.Extensions.Logging;

namespace DiscordBot.Bot.Services;

public class RoulettePublishService(BotApiClient api, ILogger<RoulettePublishService> logger)
{
    public async Task ProcessAsync(DiscordSocketClient client, CancellationToken ct)
    {
        var actions = await api.GetPendingRoulettePublishActionsAsync(ct);
        foreach (var action in actions)
        {
            try
            {
                if (!ulong.TryParse(action.DiscordGuildId, out var guildId) || !ulong.TryParse(action.ChannelDiscordId, out var channelId)) throw new InvalidOperationException("معرّف السيرفر أو الروم غير صالح.");
                var guild = client.GetGuild(guildId) ?? throw new InvalidOperationException("السيرفر غير موجود لدى البوت.");
                if (guild.GetChannel(channelId) is not IMessageChannel channel) throw new InvalidOperationException("روم الألعاب غير موجود أو ليس رومًا نصيًا.");
                IUserMessage message;
                if (action.Type == "RoomInvite")
                {
                    var embed = new EmbedBuilder().WithTitle("🎡 تحدي الروليت بدأ!")
                        .WithDescription($"🔥 {action.HostUsername} فتح تحدي روليت جديد!\nهل تقدر تصمد للنهاية؟ ادخل وتحداه الآن.")
                        .AddField("اللاعبون", $"{action.PlayersCount} / {action.MaxPlayers}", true)
                        .AddField("الحد الأدنى", action.MinPlayers, true)
                        .AddField("المكافأة", $"{action.WinnerCoins} عملة", true)
                        .AddField("مدة الانضمام", $"{action.JoinWindowSeconds} ثانية", true)
                        .WithColor(new Color(235, 69, 158)).Build();
                    var components = new ComponentBuilder().WithButton("ادخل التحدي", $"games:roulette:join:{action.RoomId:D}", ButtonStyle.Primary, new Emoji("🎡")).Build();
                    message = await channel.SendMessageAsync(embed: embed, components: components, allowedMentions: AllowedMentions.None);
                }
                else
                {
                    var embed = new EmbedBuilder().WithTitle("🏆 بطل الروليت").WithDescription($"فاز {action.WinnerUsername} في تحدي الروليت!")
                        .AddField("المكافأة", $"{action.WinnerCoins} عملة", true).AddField("عدد اللاعبين", action.PlayersCount, true)
                        .AddField("عدد الجولات", action.CurrentRound, true).WithColor(new Color(240, 178, 50)).Build();
                    message = await channel.SendMessageAsync(embed: embed, allowedMentions: AllowedMentions.None);
                }
                await api.AckRoulettePublishActionAsync(action.Id, new AckRoulettePublishActionApiRequest { Success = true, MessageDiscordId = message.Id.ToString() }, ct);
                logger.LogInformation("Published Roulette action {ActionId} type {Type} for room {RoomId}.", action.Id, action.Type, action.RoomId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Roulette publish action {ActionId} for room {RoomId} failed.", action.Id, action.RoomId);
                await api.AckRoulettePublishActionAsync(action.Id, new AckRoulettePublishActionApiRequest { Success = false, ErrorMessage = ex.Message }, ct);
            }
        }
    }
}
