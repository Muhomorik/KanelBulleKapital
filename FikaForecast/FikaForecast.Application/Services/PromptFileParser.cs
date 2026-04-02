namespace FikaForecast.Application.Services;

/// <summary>
/// Parses prompt files with a simple frontmatter format (Name metadata + body).
/// </summary>
internal static class PromptFileParser
{
    private const string FrontmatterDelimiter = "---";
    private const string NamePrefix = "Name:";

    /// <summary>
    /// Parses a prompt file into its name and system prompt body.
    /// </summary>
    /// <remarks>
    /// Expected format:
    /// <code>
    /// ---
    /// Name: Display Name Here
    /// ---
    /// Body text here...
    /// </code>
    /// If no frontmatter is found, the entire content is used as the system prompt
    /// with a fallback name.
    /// </remarks>
    public static (string Name, string SystemPrompt) Parse(string fileContent, string fallbackName = "Unnamed Prompt")
    {
        if (string.IsNullOrWhiteSpace(fileContent))
            return (fallbackName, string.Empty);

        var trimmed = fileContent.TrimStart();

        if (!trimmed.StartsWith(FrontmatterDelimiter))
            return (fallbackName, fileContent.Trim());

        // Find the closing "---" after the opening one
        var afterFirstDelimiter = trimmed.IndexOf('\n', FrontmatterDelimiter.Length);
        if (afterFirstDelimiter < 0)
            return (fallbackName, fileContent.Trim());

        var closingDelimiter = trimmed.IndexOf(
            FrontmatterDelimiter,
            afterFirstDelimiter,
            StringComparison.Ordinal);

        if (closingDelimiter < 0)
            return (fallbackName, fileContent.Trim());

        // Extract frontmatter block and body
        var frontmatter = trimmed[afterFirstDelimiter..closingDelimiter];
        var bodyStart = trimmed.IndexOf('\n', closingDelimiter);
        var body = bodyStart >= 0 ? trimmed[(bodyStart + 1)..].Trim() : string.Empty;

        // Parse Name from frontmatter
        var name = fallbackName;
        foreach (var line in frontmatter.Split('\n'))
        {
            var trimmedLine = line.Trim();
            if (trimmedLine.StartsWith(NamePrefix, StringComparison.OrdinalIgnoreCase))
            {
                name = trimmedLine[NamePrefix.Length..].Trim();
                break;
            }
        }

        return (name, body);
    }
}
