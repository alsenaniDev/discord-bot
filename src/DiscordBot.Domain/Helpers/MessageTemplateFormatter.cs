namespace DiscordBot.Domain.Helpers;

public static class MessageTemplateFormatter
{
    public static string Format(
        string template,
        IReadOnlyDictionary<string, string> tokens)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            return string.Empty;
        }

        var result = template;
        foreach (var (key, value) in tokens)
        {
            result = result.Replace(
                $"{{{key}}}",
                value ?? string.Empty,
                StringComparison.OrdinalIgnoreCase);
        }

        return result;
    }
}
