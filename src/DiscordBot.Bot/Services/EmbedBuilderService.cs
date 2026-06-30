using Discord;
using Discord.WebSocket;
using DiscordBot.Bot.Api.Models;
using DiscordBot.Bot.UI;

namespace DiscordBot.Bot.Services;

/// <summary>
/// Builds consistent embeds for bot responses.
/// </summary>
public class EmbedBuilderService
{
    private const string FooterText = "Discord Bot Platform";

    public Embed BuildError(string title, string description, string? fieldName = null, string? fieldValue = null) =>
        BuildBase(title, description, BotColors.Error, fieldName, fieldValue).Build();

    public Embed BuildSuccess(string title, string description, string? fieldName = null, string? fieldValue = null) =>
        BuildBase(title, description, BotColors.Success, fieldName, fieldValue).Build();

    public Embed BuildInfo(string title, string description, string? fieldName = null, string? fieldValue = null) =>
        BuildBase(title, description, BotColors.Info, fieldName, fieldValue).Build();

    public Embed BuildWarning(string title, string description) =>
        BuildBase(title, description, BotColors.Warning).Build();

    public Embed BuildPing(long latencyMs)
    {
        return new Discord.EmbedBuilder()
            .WithTitle("Pong!")
            .WithDescription("The bot is online and responding.")
            .WithColor(BotColors.Info)
            .AddField("Gateway latency", $"{latencyMs} ms", inline: true)
            .WithFooter(FooterText)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();
    }

    public Embed BuildServerSettings(SocketGuild guild, GuildSettingsResponse settings)
    {
        var builder = new Discord.EmbedBuilder()
            .WithTitle(guild.Name)
            .WithDescription("Current bot platform settings for this server.")
            .WithColor(BotColors.Info)
            .WithThumbnailUrl(guild.IconUrl)
            .AddField("Welcome messages", settings.WelcomeEnabled ? "Enabled" : "Disabled", inline: true)
            .AddField("Welcome channel", FormatSnowflake(settings.WelcomeChannelId), inline: true)
            .AddField("Auto role", settings.AutoRoleEnabled ? "Enabled" : "Disabled", inline: true)
            .AddField("Logs", settings.LogsEnabled ? "Enabled" : "Disabled", inline: true)
            .AddField("Tickets", settings.TicketsEnabled ? "Enabled" : "Disabled", inline: true)
            .WithFooter(FooterText)
            .WithTimestamp(DateTimeOffset.UtcNow);

        return builder.Build();
    }

    public Embed BuildGuildRegistered(RegisterGuildResponse result, SocketGuild guild)
    {
        var title = result.IsNew ? "Server registered" : "Server already registered";
        var description = result.IsNew
            ? "This server is now linked to the bot platform. Configure features from the dashboard or Discord commands."
            : "Settings were synced with the API. No duplicate record was created.";

        return new Discord.EmbedBuilder()
            .WithTitle(title)
            .WithDescription(description)
            .WithColor(result.IsNew ? BotColors.Success : BotColors.Info)
            .WithThumbnailUrl(guild.IconUrl)
            .AddField("Server", guild.Name, inline: true)
            .AddField("Platform ID", $"`{result.Id}`", inline: true)
            .AddField("Discord ID", $"`{result.DiscordGuildId}`", inline: true)
            .WithFooter(FooterText)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();
    }

    public Embed BuildSetupComplete(
        RegisterGuildResponse result,
        SocketGuild guild,
        bool resourcesSynced,
        string dashboardUrl)
    {
        var title = result.IsNew ? "Setup complete" : "Server linked";
        var description = result.IsNew
            ? $"**{guild.Name}** is registered on the bot platform."
            : $"**{guild.Name}** was already registered. Settings were refreshed.";

        var builder = new Discord.EmbedBuilder()
            .WithTitle(title)
            .WithDescription(description)
            .WithColor(BotColors.Success)
            .WithThumbnailUrl(guild.IconUrl)
            .AddField("Server registered", "Yes", inline: true)
            .AddField(
                "Resources synced",
                resourcesSynced ? "Yes — channels and roles are in the dashboard" : "Not yet — run `/sync`",
                inline: true)
            .AddField(
                "Next steps",
                "1. Open the dashboard and finish setup\n" +
                "2. Choose your plan and enable modules\n" +
                "3. Configure welcome and tickets in **Settings**",
                inline: false);

        if (!string.IsNullOrWhiteSpace(dashboardUrl))
        {
            builder.AddField("Dashboard", dashboardUrl.TrimEnd('/'), inline: false);
        }

        return builder
            .WithFooter(FooterText)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();
    }

    public Embed BuildTicketSetupSuccess(IChannel category, SocketGuild guild) =>
        new Discord.EmbedBuilder()
            .WithTitle("Ticket system enabled")
            .WithDescription("Members can open private support tickets. Share the commands below with your community.")
            .WithColor(BotColors.Success)
            .AddField("Category", category.Name, inline: true)
            .AddField("Category ID", $"`{category.Id}`", inline: true)
            .AddField("Open a ticket", "Use `/ticket open` or the **Create ticket** button.", inline: false)
            .AddField("Close a ticket", "Use `/ticket close` or the controls inside the ticket channel.", inline: false)
            .WithFooter(FooterText)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();

    public Embed BuildTicketOpenPrompt()
    {
        return new Discord.EmbedBuilder()
            .WithTitle("Open a support ticket")
            .WithDescription(
                "A private channel will be created for you and server staff. Only one open ticket is allowed per member.")
            .WithColor(BotColors.Info)
            .AddField(
                "Need help?",
                "Describe your issue inside the ticket channel after it is created.",
                inline: false)
            .WithFooter(FooterText)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();
    }

    public Embed BuildTicketCreated(ITextChannel channel, int ticketNumber)
    {
        return new Discord.EmbedBuilder()
            .WithTitle("Ticket created")
            .WithDescription($"Your private ticket channel is ready: {channel.Mention}")
            .WithColor(BotColors.Success)
            .AddField("Ticket number", $"#{ticketNumber}", inline: true)
            .AddField("Channel", channel.Mention, inline: true)
            .WithFooter(FooterText)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();
    }

    public Embed BuildTicketWelcome(SocketGuildUser owner, int ticketNumber) =>
        new Discord.EmbedBuilder()
            .WithTitle($"Ticket #{ticketNumber}")
            .WithDescription(
                $"{owner.Mention}, thanks for reaching out.\n\n" +
                "A staff member will assist you shortly. Use the button, menu, or `/ticket close` when your issue is resolved.")
            .WithColor(BotColors.Ticket)
            .AddField("Opened by", owner.Mention, inline: true)
            .AddField("Status", "Open", inline: true)
            .WithFooter(FooterText)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();

    public Embed BuildTicketClosed(int ticketNumber, IUser closedBy)
    {
        return new Discord.EmbedBuilder()
            .WithTitle("Ticket closed")
            .WithDescription(
                $"Ticket #{ticketNumber} has been closed by {closedBy.Mention}. This channel will be deleted shortly.")
            .WithColor(BotColors.Success)
            .WithFooter(FooterText)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();
    }

    public Embed BuildTicketHelp()
    {
        return new Discord.EmbedBuilder()
            .WithTitle("Ticket help")
            .WithDescription(
                "Use **Close ticket** or `/ticket close` inside your ticket channel when you are done.\n" +
                "Only the ticket owner or server staff can close a ticket.")
            .WithColor(BotColors.Info)
            .WithFooter(FooterText)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();
    }

    public Embed BuildWelcome(SocketGuildUser user, SocketGuild guild, string message) =>
        new Discord.EmbedBuilder()
            .WithDescription(message)
            .WithColor(BotColors.Success)
            .WithAuthor(user.Username, user.GetDisplayAvatarUrl())
            .WithFooter($"Welcome to {guild.Name}")
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();

    public Embed BuildReactionRolePanel(string title, string description, SocketRole role) =>
        new Discord.EmbedBuilder()
            .WithTitle(title)
            .WithDescription(description)
            .WithColor(role.Color != Color.Default ? role.Color : BotColors.Info)
            .AddField("Role", role.Mention, inline: true)
            .AddField("How it works", "Click the button below to toggle this role on or off.", inline: false)
            .WithFooter(FooterText)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();

    private static Discord.EmbedBuilder BuildBase(
        string title,
        string description,
        Color color,
        string? fieldName = null,
        string? fieldValue = null)
    {
        var builder = new Discord.EmbedBuilder()
            .WithTitle(title)
            .WithDescription(description)
            .WithColor(color)
            .WithFooter(FooterText)
            .WithTimestamp(DateTimeOffset.UtcNow);

        if (!string.IsNullOrWhiteSpace(fieldName) && !string.IsNullOrWhiteSpace(fieldValue))
        {
            builder.AddField(fieldName, fieldValue, inline: false);
        }

        return builder;
    }

    private static string FormatSnowflake(string? snowflake) =>
        string.IsNullOrWhiteSpace(snowflake) ? "Not set" : $"`{snowflake}`";
}
