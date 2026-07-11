namespace DiscordBot.Activities.Application.Realtime;

public static class GameSessionGroupNames
{
    public static string Roulette(Guid gameSessionId) => $"game-session:{gameSessionId}";
}
