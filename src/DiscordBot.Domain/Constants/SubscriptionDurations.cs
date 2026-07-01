namespace DiscordBot.Domain.Constants;

public static class SubscriptionDurations
{
    public static readonly int[] AllowedMonths = [1, 3, 6, 12];

    public static bool IsValid(int months) => AllowedMonths.Contains(months);
}
