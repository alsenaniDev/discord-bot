using Discord;

namespace DiscordBot.Bot.Commands;

public sealed record TicketCreateResult(
    bool Success,
    ITextChannel? Channel = null,
    int TicketNumber = 0,
    string? ErrorTitle = null,
    string? ErrorDescription = null);

public sealed record TicketCloseResult(
    bool Success,
    int TicketNumber = 0,
    string? ErrorTitle = null,
    string? ErrorDescription = null);
