using Discord;
using Discord.WebSocket;
using DiscordBot.Bot.Services;

namespace DiscordBot.Bot.UI;

/// <summary>
/// Consistent interaction responses using embeds.
/// </summary>
public static class InteractionResponseHelper
{
    public static Task RespondErrorAsync(
        SocketInteraction interaction,
        EmbedBuilderService embeds,
        string title,
        string description,
        bool ephemeral = true) =>
        interaction.RespondAsync(
            embed: embeds.BuildError(title, description),
            ephemeral: ephemeral);

    public static Task RespondSuccessAsync(
        SocketInteraction interaction,
        EmbedBuilderService embeds,
        string title,
        string description,
        bool ephemeral = true,
        MessageComponent? components = null) =>
        interaction.RespondAsync(
            embed: embeds.BuildSuccess(title, description),
            components: components,
            ephemeral: ephemeral);

    public static Task RespondInfoAsync(
        SocketInteraction interaction,
        EmbedBuilderService embeds,
        string title,
        string description,
        bool ephemeral = true,
        MessageComponent? components = null) =>
        interaction.RespondAsync(
            embed: embeds.BuildInfo(title, description),
            components: components,
            ephemeral: ephemeral);

    public static Task FollowupErrorAsync(
        SocketInteraction interaction,
        EmbedBuilderService embeds,
        string title,
        string description,
        bool ephemeral = true) =>
        interaction.FollowupAsync(
            embed: embeds.BuildError(title, description),
            ephemeral: ephemeral);

    public static Task FollowupSuccessAsync(
        SocketInteraction interaction,
        EmbedBuilderService embeds,
        string title,
        string description,
        bool ephemeral = true) =>
        interaction.FollowupAsync(
            embed: embeds.BuildSuccess(title, description),
            ephemeral: ephemeral);

    public static Task RespondUnexpectedErrorAsync(
        SocketInteraction interaction,
        EmbedBuilderService embeds,
        bool ephemeral = true)
    {
        if (interaction.HasResponded)
        {
            return interaction.FollowupAsync(
                embed: embeds.BuildError(
                    "Something went wrong",
                    "An unexpected error occurred. Please try again in a moment."),
                ephemeral: ephemeral);
        }

        return RespondErrorAsync(
            interaction,
            embeds,
            "Something went wrong",
            "An unexpected error occurred. Please try again in a moment.",
            ephemeral);
    }
}
