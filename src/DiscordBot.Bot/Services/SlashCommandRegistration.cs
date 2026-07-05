using Discord;
using Discord.WebSocket;

namespace DiscordBot.Bot.Services;

public static class SlashCommandRegistration
{
    public static async Task RegisterGlobalCommandsAsync(DiscordSocketClient client)
    {
        var commands = BuildCommands();
        await client.BulkOverwriteGlobalApplicationCommandsAsync(commands);
    }

    /// <summary>
    /// Guild commands appear instantly — useful during local development.
    /// </summary>
    public static async Task RegisterGuildCommandsAsync(SocketGuild guild)
    {
        var commands = BuildCommands();
        await guild.BulkOverwriteApplicationCommandAsync(commands);
    }

    private static ApplicationCommandProperties[] BuildCommands() =>
    [
        new SlashCommandBuilder()
            .WithName("ping")
            .WithDescription("Check if the bot is alive")
            .Build(),
        new SlashCommandBuilder()
            .WithName("server")
            .WithDescription("Show server info and bot settings")
            .Build(),
        new SlashCommandBuilder()
            .WithName("setup")
            .WithDescription("Register this server with the bot platform")
            .Build(),
        new SlashCommandBuilder()
            .WithName("sync")
            .WithDescription("Sync Discord channels and roles with the dashboard")
            .Build(),
        new SlashCommandBuilder()
            .WithName("ticket")
            .WithDescription("Support ticket commands")
            .AddOption("setup", ApplicationCommandOptionType.SubCommand, "Create a ticket category and enable tickets")
            .AddOption("open", ApplicationCommandOptionType.SubCommand, "Open a private support ticket")
            .AddOption("close", ApplicationCommandOptionType.SubCommand, "Close the current ticket")
            .Build(),
        new SlashCommandBuilder()
            .WithName("warn")
            .WithDescription("Warn a member")
            .AddOption("user", ApplicationCommandOptionType.User, "Member to warn", isRequired: true)
            .AddOption("reason", ApplicationCommandOptionType.String, "Reason for the warning", isRequired: true)
            .Build(),
        new SlashCommandBuilder()
            .WithName("warnings")
            .WithDescription("View warnings for a member")
            .AddOption("user", ApplicationCommandOptionType.User, "Member to look up", isRequired: true)
            .Build(),
        new SlashCommandBuilder()
            .WithName("clear")
            .WithDescription("Bulk delete recent messages in this channel")
            .AddOption("amount", ApplicationCommandOptionType.Integer, "Number of messages (1-100)", isRequired: true)
            .Build(),
        new SlashCommandBuilder()
            .WithName("kick")
            .WithDescription("Kick a member from the server")
            .AddOption("user", ApplicationCommandOptionType.User, "Member to kick", isRequired: true)
            .AddOption("reason", ApplicationCommandOptionType.String, "Reason for the kick", isRequired: true)
            .Build(),
        new SlashCommandBuilder()
            .WithName("music")
            .WithDescription("Play and control music")
            .AddOption(new SlashCommandOptionBuilder().WithName("play").WithDescription("Play or queue a track").WithType(ApplicationCommandOptionType.SubCommand).AddOption("query", ApplicationCommandOptionType.String, "Audio URL or search query", isRequired: true))
            .AddOption("skip", ApplicationCommandOptionType.SubCommand, "Skip the current track")
            .AddOption("stop", ApplicationCommandOptionType.SubCommand, "Stop playback and clear the queue")
            .AddOption("pause", ApplicationCommandOptionType.SubCommand, "Pause playback")
            .AddOption("resume", ApplicationCommandOptionType.SubCommand, "Resume playback")
            .AddOption("queue", ApplicationCommandOptionType.SubCommand, "Show the music queue")
            .AddOption("nowplaying", ApplicationCommandOptionType.SubCommand, "Show the current track")
            .Build(),
        new SlashCommandBuilder()
            .WithName("games")
            .WithDescription("افتح مركز الألعاب والتحديات")
            .Build(),
        new SlashCommandBuilder()
            .WithName("reaction-role")
            .WithDescription("Create button-based role panels")
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("create")
                .WithDescription("Post a reaction role panel in a channel")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("channel", ApplicationCommandOptionType.Channel, "Channel to post in", isRequired: true)
                .AddOption("title", ApplicationCommandOptionType.String, "Panel title", isRequired: true)
                .AddOption("description", ApplicationCommandOptionType.String, "Panel description", isRequired: true)
                .AddOption("role", ApplicationCommandOptionType.Role, "Role to toggle", isRequired: true)
                .AddOption("button_label", ApplicationCommandOptionType.String, "Button label", isRequired: true))
            .Build()
    ];
}
