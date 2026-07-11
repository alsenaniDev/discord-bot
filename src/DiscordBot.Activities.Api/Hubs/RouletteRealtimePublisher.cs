using DiscordBot.Activities.Application.Abstractions;
using DiscordBot.Activities.Application.Models;
using DiscordBot.Activities.Application.Realtime;
using Microsoft.AspNetCore.SignalR;

namespace DiscordBot.Activities.Api.Hubs;

public class RouletteRealtimePublisher(IHubContext<GameHub> hub, ILogger<RouletteRealtimePublisher> logger) : IRouletteRealtimePublisher
{
    public async Task PublishAsync(RouletteRealtimeEvent evt, CancellationToken ct = default)
    {
        var group = GameSessionGroupNames.Roulette(evt.GameSessionId);
        await hub.Clients.Group(group).SendAsync(evt.Type, evt.Payload, ct);
        logger.LogDebug("Published Roulette realtime event {EventType} to {GroupName}.", evt.Type, group);
    }
}
