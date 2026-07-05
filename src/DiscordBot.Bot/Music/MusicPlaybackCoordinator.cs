using Discord.WebSocket;
using DiscordBot.Bot.Configuration;
using Lavalink4NET;
using Lavalink4NET.Events.Players;
using Lavalink4NET.Players.Queued;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DiscordBot.Bot.Music;

public sealed class MusicPlaybackCoordinator(
    IAudioService audio,
    MusicSessionManager sessions,
    DiscordSocketClient discord,
    IOptions<LavalinkOptions> options,
    ILogger<MusicPlaybackCoordinator> logger) : BackgroundService
{
    public override Task StartAsync(CancellationToken cancellationToken)
    {
        audio.TrackStarted += OnTrackStartedAsync;
        audio.TrackEnded += OnTrackEndedAsync;
        audio.TrackException += OnTrackExceptionAsync;
        return base.StartAsync(cancellationToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        audio.TrackStarted -= OnTrackStartedAsync;
        audio.TrackEnded -= OnTrackEndedAsync;
        audio.TrackException -= OnTrackExceptionAsync;
        return base.StopAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var cutoff = DateTimeOffset.UtcNow.AddSeconds(-Math.Max(10, options.Value.IdleTimeoutSeconds));
            foreach (var session in sessions.Snapshots().Where(x => x.IdleSinceUtc <= cutoff))
            {
                var active = sessions.GetSession(session.GuildId); if (active is null) continue;
                await active.OperationGate.WaitAsync(stoppingToken);
                try
                {
                    if (sessions.Snapshot(session.GuildId)?.IdleSinceUtc is not { } idleSince || idleSince > cutoff) continue;
                    var player = await audio.Players.GetPlayerAsync<IQueuedLavalinkPlayer>(session.GuildId, stoppingToken);
                    if (player is not null) await player.DisconnectAsync(stoppingToken);
                    sessions.RemoveSession(session.GuildId, out _);
                    if (discord.GetGuild(session.GuildId)?.GetTextChannel(session.TextChannelId) is { } channel)
                        await channel.SendMessageAsync("The music queue ended, so I left the voice channel.");
                    logger.LogInformation("Idle music session removed in guild {GuildId}.", session.GuildId);
                }
                catch (Exception ex) { logger.LogError(ex, "Failed to clean idle music session in guild {GuildId}.", session.GuildId); }
                finally { active.OperationGate.Release(); }
            }
        }
    }

    private Task OnTrackStartedAsync(object sender, TrackStartedEventArgs args)
    {
        sessions.TrackStarted(args.Player.GuildId, args.Track.ToString());
        logger.LogInformation("Music track started in guild {GuildId}: {Title}.", args.Player.GuildId, args.Track.Title);
        return Task.CompletedTask;
    }
    private Task OnTrackEndedAsync(object sender, TrackEndedEventArgs args)
    {
        sessions.TrackEnded(args.Player.GuildId);
        return Task.CompletedTask;
    }
    private Task OnTrackExceptionAsync(object sender, TrackExceptionEventArgs args)
    {
        sessions.TrackEnded(args.Player.GuildId);
        logger.LogError("Lavalink track error in guild {GuildId} for track {Title}.", args.Player.GuildId, args.Track.Title);
        return Task.CompletedTask;
    }
}
