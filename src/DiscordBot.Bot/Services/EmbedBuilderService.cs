using Discord;
using Discord.WebSocket;
using DiscordBot.Bot.Api.Models;
using DiscordBot.Bot.UI;
using DiscordBot.Domain.Constants;
using DiscordBot.Domain.Helpers;

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

    public Embed BuildTicketWelcome(SocketGuildUser owner, int ticketNumber, GuildSettingsResponse settings)
    {
        var tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ticket"] = ticketNumber.ToString(),
            ["mention"] = owner.Mention,
            ["user"] = owner.Mention,
            ["username"] = owner.Username
        };

        var title = MessageTemplateFormatter.Format(
            string.IsNullOrWhiteSpace(settings.TicketWelcomeTitle)
                ? TicketMessageDefaults.WelcomeTitle
                : settings.TicketWelcomeTitle,
            tokens);

        var description = MessageTemplateFormatter.Format(
            string.IsNullOrWhiteSpace(settings.TicketWelcomeMessage)
                ? TicketMessageDefaults.WelcomeMessage
                : settings.TicketWelcomeMessage,
            tokens);

        return new Discord.EmbedBuilder()
            .WithTitle(title)
            .WithDescription(description)
            .WithColor(BotColors.Ticket)
            .AddField("Opened by", owner.Mention, inline: true)
            .AddField("Status", "Open", inline: true)
            .WithFooter(FooterText)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();
    }

    public Embed BuildTicketClosedFromDashboard(int ticketNumber, string messageTemplate)
    {
        var description = MessageTemplateFormatter.Format(
            string.IsNullOrWhiteSpace(messageTemplate)
                ? TicketMessageDefaults.ClosedFromDashboardMessage
                : messageTemplate,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["ticket"] = ticketNumber.ToString()
            });

        return new Discord.EmbedBuilder()
            .WithTitle("Ticket closed")
            .WithDescription(description)
            .WithColor(BotColors.Warning)
            .WithFooter(FooterText)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();
    }

    public Embed BuildTicketClosed(int ticketNumber, IUser closedBy, GuildSettingsResponse settings)
    {
        var description = MessageTemplateFormatter.Format(
            string.IsNullOrWhiteSpace(settings.TicketClosedMessage)
                ? TicketMessageDefaults.ClosedMessage
                : settings.TicketClosedMessage,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["ticket"] = ticketNumber.ToString(),
                ["mention"] = closedBy.Mention,
                ["user"] = closedBy.Mention,
                ["username"] = closedBy.Username
            });

        return new Discord.EmbedBuilder()
            .WithTitle("Ticket closed")
            .WithDescription(description)
            .WithColor(BotColors.Success)
            .WithFooter(FooterText)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();
    }

    public Embed BuildCommandPanel(string title, string description, string? imageUrl = null)
    {
        var builder = new Discord.EmbedBuilder()
            .WithTitle(title)
            .WithDescription(description)
            .WithColor(BotColors.Info)
            .WithFooter(FooterText)
            .WithTimestamp(DateTimeOffset.UtcNow);

        if (!string.IsNullOrWhiteSpace(imageUrl)
            && IsValidPanelImageUrl(imageUrl))
        {
            builder.WithImageUrl(imageUrl.Trim());
        }

        return builder.Build();
    }

    private static bool IsValidPanelImageUrl(string url) =>
        Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    public Embed BuildTicketArchive(
        int ticketNumber,
        string openerName,
        string openerId,
        string closedByName,
        string closedById,
        DateTimeOffset closedAt,
        string digestPreview,
        string? transcriptUrl = null)
    {
        var builder = new Discord.EmbedBuilder()
            .WithTitle($"Ticket #{ticketNumber} closed")
            .WithDescription(
                "_This is an **archive digest** — a short summary for Discord notification. "
                + "It is not the full ticket transcript._\n\n"
                + digestPreview)
            .WithColor(BotColors.Info)
            .AddField("Opened by", $"{openerName}\n`{openerId}`", inline: true)
            .AddField("Closed by", $"{closedByName}\n`{closedById}`", inline: true)
            .AddField("Closed at", $"<t:{closedAt.ToUnixTimeSeconds()}:F>", inline: true);

        if (!string.IsNullOrWhiteSpace(transcriptUrl))
        {
            builder.AddField("Full transcript", $"[Open in Dashboard]({transcriptUrl})", inline: false);
        }
        else
        {
            builder.AddField(
                "Full transcript",
                "Available in the Dashboard for authorized staff.",
                inline: false);
        }

        return builder
            .WithFooter(FooterText)
            .WithTimestamp(closedAt)
            .Build();
    }

    public Embed BuildServerProfile(SocketGuild guild, GuildProfileApiResponse? profile, GuildSettingsResponse settings)
    {
        var displayName = profile?.DisplayName ?? guild.Name;
        var builder = new Discord.EmbedBuilder()
            .WithTitle(displayName)
            .WithColor(BotColors.Info)
            .WithThumbnailUrl(guild.IconUrl)
            .AddField("Welcome messages", settings.WelcomeEnabled ? "Enabled" : "Disabled", inline: true)
            .AddField("Auto role", settings.AutoRoleEnabled ? "Enabled" : "Disabled", inline: true)
            .AddField("Logs", settings.LogsEnabled ? "Enabled" : "Disabled", inline: true)
            .AddField("Tickets", settings.TicketsEnabled ? "Enabled" : "Disabled", inline: true)
            .WithFooter(FooterText)
            .WithTimestamp(DateTimeOffset.UtcNow);

        if (!string.IsNullOrWhiteSpace(profile?.Description))
        {
            builder.WithDescription(profile.Description);
        }

        if (!string.IsNullOrWhiteSpace(profile?.CommunityType))
        {
            builder.AddField("Community type", profile.CommunityType, inline: true);
        }

        if (!string.IsNullOrWhiteSpace(profile?.SupportMessage))
        {
            builder.AddField("Support", profile.SupportMessage, inline: false);
        }

        if (!string.IsNullOrWhiteSpace(profile?.WebsiteUrl))
        {
            builder.AddField("Website", profile.WebsiteUrl, inline: true);
        }

        if (!string.IsNullOrWhiteSpace(profile?.RulesUrl))
        {
            builder.AddField("Rules", profile.RulesUrl, inline: true);
        }

        return builder.Build();
    }

    public Embed BuildCommandPanelLegacy(string title, string description) =>
        BuildCommandPanel(title, description);

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

    public Embed BuildModerationHelp()
    {
        return new Discord.EmbedBuilder()
            .WithTitle("Moderation help")
            .WithDescription("Staff can use these slash commands in Discord:")
            .WithColor(BotColors.Info)
            .AddField("Warn a member", "`/warn user:<member> reason:<text>`", inline: false)
            .AddField("View warnings", "`/warnings user:<member>`", inline: false)
            .AddField("Kick a member", "`/kick user:<member> reason:<text>`", inline: false)
            .AddField("Clear messages", "`/clear amount:<1-100>`", inline: false)
            .WithFooter(FooterText)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();
    }

    public Embed BuildReactionRolesHelp()
    {
        return new Discord.EmbedBuilder()
            .WithTitle("Reaction roles help")
            .WithDescription(
                "Reaction role panels let members toggle roles with a button.\n" +
                "Staff can create panels with `/reaction-role create` in Discord.")
            .WithColor(BotColors.Info)
            .AddField("How it works", "Click a panel button to add or remove the linked role.", inline: false)
            .WithFooter(FooterText)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();
    }

    public Embed BuildPlatformHelp(string dashboardUrl)
    {
        var builder = new Discord.EmbedBuilder()
            .WithTitle("Bot platform help")
            .WithDescription(
                "Manage welcome messages, tickets, moderation, modules, and more from the dashboard.\n" +
                "Use `/sync` in Discord after changing channels or roles.")
            .WithColor(BotColors.Info)
            .AddField("Useful commands", "`/ping` · `/server` · `/ticket open` · `/sync`", inline: false);

        if (!string.IsNullOrWhiteSpace(dashboardUrl))
        {
            builder.AddField("Dashboard", dashboardUrl.TrimEnd('/'), inline: false);
        }

        return builder
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
            .WithColor(role.Colors.PrimaryColor != Color.Default ? role.Colors.PrimaryColor : BotColors.Info)
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
