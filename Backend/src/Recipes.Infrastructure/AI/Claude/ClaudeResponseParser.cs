using Recipes.Infrastructure.AI.Claude.Models;

namespace Recipes.Infrastructure.AI.Claude;

internal static class ClaudeResponseParser
{
    internal static string ExtractText(ClaudeMessagesResponse response)
    {
        var textBlocks = response.Content
            .Where(x => string.Equals(x.Type, "text", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Text)
            .Where(x => !string.IsNullOrWhiteSpace(x));

        return string.Join("\n", textBlocks!);
    }

    internal static string StripMarkdownFences(string text)
    {
        var trimmed = text.Trim();

        if (trimmed.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed[7..].Trim();
        else if (trimmed.StartsWith("```", StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed[3..].Trim();

        if (trimmed.EndsWith("```", StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed[..^3].Trim();

        return trimmed;
    }
}
