namespace DiscordBot.Activities.Domain.Roulette;

public static class RouletteRuntimeStates
{
    public const string WaitingForPlayers = "Waiting";
    public const string BettingOpen = "InProgress";
    public const string BettingClosed = "BettingClosed";
    public const string Spinning = "Spinning";
    public const string Settling = "Settling";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";
    public const string Expired = "Expired";
    public const string Abandoned = "Abandoned";

    public static bool CanTransition(string current, string next)
    {
        if (current == next) return true;
        return current switch
        {
            WaitingForPlayers => next is BettingOpen or Cancelled or Expired,
            BettingOpen => next is Spinning or Settling or Completed or Cancelled or Abandoned,
            BettingClosed => next is Spinning or Cancelled,
            Spinning => next is Settling or Cancelled,
            Settling => next is BettingOpen or Completed or Cancelled,
            Completed or Cancelled or Expired or Abandoned => false,
            _ => false
        };
    }

    public static bool IsOpenForJoin(string status) => status == WaitingForPlayers;
    public static bool IsPlayable(string status) => status is BettingOpen or Spinning or Settling;
    public static bool IsTerminal(string status) => status is Completed or Cancelled or Expired or Abandoned;
}
