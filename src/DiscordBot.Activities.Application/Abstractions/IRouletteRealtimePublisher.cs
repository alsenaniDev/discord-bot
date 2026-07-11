using DiscordBot.Activities.Application.Models;

namespace DiscordBot.Activities.Application.Abstractions;

public interface IRouletteRealtimePublisher
{
    Task PublishAsync(RouletteRealtimeEvent evt, CancellationToken ct = default);
}
