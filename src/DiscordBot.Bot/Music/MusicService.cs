using Discord;
using Discord.WebSocket;
using DiscordBot.Bot.Api;
using DiscordBot.Bot.Api.Models;
using DiscordBot.Bot.UI;
using Lavalink4NET;
using Lavalink4NET.Extensions;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Queued;
using Lavalink4NET.Tracks;
using Microsoft.Extensions.Logging;

namespace DiscordBot.Bot.Music;

public interface IMusicService
{
    Task PlayAsync(SocketInteraction interaction, string query);
    Task SkipAsync(SocketInteraction interaction);
    Task StopAsync(SocketInteraction interaction);
    Task PauseAsync(SocketInteraction interaction);
    Task ResumeAsync(SocketInteraction interaction);
    Task ShowQueueAsync(SocketInteraction interaction);
    Task ShowNowPlayingAsync(SocketInteraction interaction);
    Task HandleButtonAsync(SocketMessageComponent component, string action);
}

public sealed class MusicService(
    BotApiClient api,
    IAudioService audio,
    IAudioSearchService search,
    MusicSessionManager sessions,
    ILogger<MusicService> logger) : IMusicService
{
    public async Task PlayAsync(SocketInteraction interaction, string query)
    {
        var member = await GetMemberAsync(interaction); if (member is null) return;
        var settings = await GetEnabledSettingsAsync(interaction, member.Guild.Id); if (settings is null) return;
        var voice = member.VoiceChannel; if (voice is null) { await ErrorAsync(interaction, "Join a voice channel before playing music."); return; }
        var botPermissions = member.Guild.CurrentUser.GetPermissions(voice);
        if (!botPermissions.ViewChannel || !botPermissions.Connect || !botPermissions.Speak) { await ErrorAsync(interaction, "I need View Channel, Connect, and Speak permissions in your voice channel."); return; }
        if (!CanQueue(member, settings, voice.Id)) { await ErrorAsync(interaction, "You are not allowed to queue music in this channel."); return; }
        if (sessions.HasSessionInDifferentVoiceChannel(member.Guild.Id, voice.Id)) { await ErrorAsync(interaction, "Music is already playing in another voice channel. Join that channel or stop the current session first."); return; }
        var existing = sessions.GetSession(member.Guild.Id);
        if (existing is not null && sessions.PendingCount(existing) >= settings.MaxQueueSize) { await ErrorAsync(interaction, "The music queue is full."); return; }

        await interaction.DeferAsync(ephemeral: true);
        MusicTrack? track;
        try { track = (await search.SearchAsync(query.Trim(), CancellationToken.None)).FirstOrDefault(); }
        catch (AudioProviderUnavailableException) { await ErrorAsync(interaction, "The music service is unavailable right now. Please try again later."); return; }
        if (track is null) { await ErrorAsync(interaction, "No playable track was found for that query."); return; }
        if (track.Duration is { } duration && duration.TotalSeconds > settings.MaxTrackDurationSeconds) { await ErrorAsync(interaction, $"That track is longer than the {settings.MaxTrackDurationSeconds / 60}-minute server limit."); return; }
        track.RequestedByUserId = member.Id; track.RequestedByUsername = member.DisplayName;

        var session = sessions.GetOrCreateSession(member.Guild.Id, voice.Id, interaction.Channel.Id, settings.DefaultVolume, out var created);
        if (session.VoiceChannelId != voice.Id) { await ErrorAsync(interaction, "Music started in another voice channel. Join that channel or stop it first."); return; }
        session.TextChannelId = interaction.Channel.Id;
        await session.OperationGate.WaitAsync();
        try
        {
            if (sessions.PendingCount(session) >= settings.MaxQueueSize) { await ErrorAsync(interaction, "The music queue is full."); return; }
            var player = await GetOrJoinPlayerAsync(member.Guild.Id, voice.Id, settings.DefaultVolume);
            sessions.EnqueueTrack(session, track);
            try { await player.PlayAsync(LavalinkTrack.Parse(track.ProviderTrackId, provider: null), enqueue: true); }
            catch { sessions.RemoveQueuedTrack(session, track.ProviderTrackId); throw; }
            if (created) logger.LogInformation("Music session started in guild {GuildId}, voice channel {VoiceChannelId}.", member.Guild.Id, voice.Id);
            logger.LogInformation("Music track queued in guild {GuildId}: {Title}, requested by {UserId}.", member.Guild.Id, track.Title, member.Id);
            try { await interaction.Channel.SendMessageAsync(embed: BuildTrackEmbed(created ? "Now playing" : "Added to queue", track), components: BuildControls(session.IsPaused)); }
            catch (Exception ex) { logger.LogWarning(ex, "Could not send music announcement in guild {GuildId}, text channel {ChannelId}.", member.Guild.Id, interaction.Channel.Id); }
            await SuccessAsync(interaction, created ? "Playback started." : "Track added to the queue.");
        }
        catch (Exception ex)
        {
            if (created)
            {
                sessions.RemoveSession(member.Guild.Id, out _);
                try { var player = await audio.Players.GetPlayerAsync<IQueuedLavalinkPlayer>(member.Guild.Id); if (player is not null) await player.DisconnectAsync(); }
                catch (Exception cleanupError) { logger.LogWarning(cleanupError, "Could not disconnect failed music session in guild {GuildId}.", member.Guild.Id); }
            }
            logger.LogError(ex, "Lavalink playback failed in guild {GuildId}, channel {VoiceChannelId}.", member.Guild.Id, voice.Id);
            await ErrorAsync(interaction, "The audio service could not start playback. Please try again later.");
        }
        finally { session.OperationGate.Release(); }
    }

    public Task SkipAsync(SocketInteraction interaction) => ControlAsync(interaction, "skip");
    public Task StopAsync(SocketInteraction interaction) => ControlAsync(interaction, "stop");
    public Task PauseAsync(SocketInteraction interaction) => ControlAsync(interaction, "pause");
    public Task ResumeAsync(SocketInteraction interaction) => ControlAsync(interaction, "resume");

    public async Task ShowQueueAsync(SocketInteraction interaction)
    {
        var member = await GetMemberAsync(interaction); if (member is null) return;
        var snapshot = sessions.Snapshot(member.Guild.Id); if (snapshot is null) { await ErrorAsync(interaction, "There is no active music session."); return; }
        var lines = snapshot.Queue.Take(15).Select((x, i) => $"`{i + 1}.` **{Escape(x.Title)}** — {Escape(x.RequestedByUsername)}").ToList();
        var description = lines.Count == 0 ? "The queue is empty." : string.Join('\n', lines);
        await interaction.RespondAsync(embed: new EmbedBuilder().WithTitle("Music queue").WithDescription(description).WithColor(Color.Blue).Build(), ephemeral: false);
    }

    public async Task ShowNowPlayingAsync(SocketInteraction interaction)
    {
        var member = await GetMemberAsync(interaction); if (member is null) return;
        var snapshot = sessions.Snapshot(member.Guild.Id); if (snapshot?.CurrentTrack is null) { await ErrorAsync(interaction, "Nothing is playing right now."); return; }
        await interaction.RespondAsync(embed: BuildTrackEmbed(snapshot.IsPaused ? "Paused" : "Now playing", snapshot.CurrentTrack), components: BuildControls(snapshot.IsPaused));
    }

    public Task HandleButtonAsync(SocketMessageComponent component, string action) => action == "queue" ? ShowQueueAsync(component) : ControlAsync(component, action);

    private async Task ControlAsync(SocketInteraction interaction, string action)
    {
        var member = await GetMemberAsync(interaction); if (member is null) return;
        var settings = await GetEnabledSettingsAsync(interaction, member.Guild.Id); if (settings is null) return;
        var session = sessions.GetSession(member.Guild.Id); if (session is null) { await ErrorAsync(interaction, "There is no active music session."); return; }
        if (!CanControl(member, settings, session.VoiceChannelId)) { await ErrorAsync(interaction, "You are not allowed to control this music session."); return; }
        await session.OperationGate.WaitAsync();
        try
        {
            var player = await audio.Players.GetPlayerAsync<IQueuedLavalinkPlayer>(member.Guild.Id);
            if (player is null) { sessions.RemoveSession(member.Guild.Id, out _); await ErrorAsync(interaction, "The music session is no longer connected."); return; }
            switch (action)
            {
                case "skip": await player.SkipAsync(); logger.LogInformation("Music track skipped in guild {GuildId} by {UserId}.", member.Guild.Id, member.Id); break;
                case "stop": await player.StopAsync(); await player.DisconnectAsync(); sessions.RemoveSession(member.Guild.Id, out _); logger.LogInformation("Music session stopped in guild {GuildId} by {UserId}.", member.Guild.Id, member.Id); break;
                case "pause": await player.PauseAsync(); session.IsPaused = true; break;
                case "resume": await player.ResumeAsync(); session.IsPaused = false; break;
                default: await ErrorAsync(interaction, "Unknown music control."); return;
            }
            session.LastActivityUtc = DateTimeOffset.UtcNow;
            var message = action switch { "skip" => "Skipped the current track.", "stop" => "Stopped playback and cleared the queue.", "pause" => "Playback paused.", _ => "Playback resumed." };
            if (interaction is SocketMessageComponent component)
            {
                await component.UpdateAsync(x => x.Components = action == "stop" ? new ComponentBuilder().Build() : BuildControls(session.IsPaused));
                await component.FollowupAsync(message, ephemeral: true);
            }
            else await RespondAsync(interaction, message, ephemeral: false);
        }
        catch (Exception ex) { logger.LogError(ex, "Music control {Action} failed in guild {GuildId}.", action, member.Guild.Id); await ErrorAsync(interaction, "The music control could not be completed."); }
        finally { session.OperationGate.Release(); }
    }

    private async Task<IQueuedLavalinkPlayer> GetOrJoinPlayerAsync(ulong guildId, ulong voiceChannelId, int volume)
    {
        var existing = await audio.Players.GetPlayerAsync<IQueuedLavalinkPlayer>(guildId); if (existing is not null) return existing;
        return await audio.Players.JoinAsync(guildId, voiceChannelId, PlayerFactory.Queued, options => { options.InitialVolume = volume / 100f; options.SelfDeaf = true; options.DisconnectOnStop = false; });
    }

    private async Task<GuildMusicSettingsResponse?> GetEnabledSettingsAsync(SocketInteraction interaction, ulong guildId)
    {
        var settings = await api.GetMusicSettingsAsync(guildId.ToString());
        if (settings is null) { await ErrorAsync(interaction, "Music settings could not be loaded."); return null; }
        if (!settings.IsEnabled) { await ErrorAsync(interaction, "Music is disabled for this server."); return null; }
        return settings;
    }

    private static bool CanQueue(SocketGuildUser user, GuildMusicSettingsResponse settings, ulong voiceChannelId)
    {
        if (user.GuildPermissions.Administrator) return true;
        if (!string.IsNullOrWhiteSpace(settings.DjRoleDiscordId)) return ulong.TryParse(settings.DjRoleDiscordId, out var roleId) && user.Roles.Any(x => x.Id == roleId);
        return settings.AllowEveryoneToQueue && user.VoiceChannel?.Id == voiceChannelId;
    }
    private static bool CanControl(SocketGuildUser user, GuildMusicSettingsResponse settings, ulong voiceChannelId)
    {
        if (user.GuildPermissions.Administrator) return true;
        if (!string.IsNullOrWhiteSpace(settings.DjRoleDiscordId)) return ulong.TryParse(settings.DjRoleDiscordId, out var roleId) && user.Roles.Any(x => x.Id == roleId);
        return user.VoiceChannel?.Id == voiceChannelId;
    }
    private static async Task<SocketGuildUser?> GetMemberAsync(SocketInteraction interaction)
    {
        if (interaction.User is SocketGuildUser member) return member;
        await ErrorAsync(interaction, "Music commands can only be used inside a server."); return null;
    }
    private static Embed BuildTrackEmbed(string title, MusicTrack track) => new EmbedBuilder().WithTitle(title).WithDescription($"**[{Escape(track.Title)}]({track.Url})**\n{Escape(track.Author ?? "Unknown artist")}\nRequested by {Escape(track.RequestedByUsername)}").WithColor(Color.Purple).Build();
    private static MessageComponent BuildControls(bool paused) => new ComponentBuilder()
        .WithButton(paused ? "Resume" : "Pause", paused ? DiscordCustomIds.MusicResume : DiscordCustomIds.MusicPause, ButtonStyle.Primary)
        .WithButton("Skip", DiscordCustomIds.MusicSkip, ButtonStyle.Secondary).WithButton("Stop", DiscordCustomIds.MusicStop, ButtonStyle.Danger)
        .WithButton("Queue", DiscordCustomIds.MusicQueue, ButtonStyle.Secondary).Build();
    private static string Escape(string value) => Format.Sanitize(value);
    private static Task SuccessAsync(SocketInteraction interaction, string text) => RespondAsync(interaction, text, true);
    private static Task ErrorAsync(SocketInteraction interaction, string text) => RespondAsync(interaction, text, true);
    private static Task RespondAsync(SocketInteraction interaction, string text, bool ephemeral) => interaction.HasResponded ? interaction.FollowupAsync(text, ephemeral: ephemeral) : interaction.RespondAsync(text, ephemeral: ephemeral);
}
