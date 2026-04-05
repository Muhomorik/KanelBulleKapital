using System.Text;
using FikaForecast.Application.DTOs;
using FikaForecast.Domain.Enums;

namespace FikaForecast.Application.Services;

/// <summary>
/// Renders a <see cref="NewsBriefParseResult"/> into display markdown with emojis
/// for the WebView2 UI.
/// </summary>
public class NewsBriefMarkdownRenderer
{
    /// <summary>
    /// Renders the parse result as emoji markdown, grouped by category.
    /// </summary>
    public string Render(NewsBriefParseResult parseResult)
    {
        if (parseResult.Item is null || parseResult.Item.Assessments.Count == 0)
            return string.Empty;

        var item = parseResult.Item;
        var sb = new StringBuilder();

        sb.AppendLine($"MARKET-MOVING NEWS BRIEF — {DateTimeOffset.UtcNow:MMMM dd, yyyy}");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();

        // Group assessments by category, preserving order of first appearance
        var grouped = item.Assessments
            .GroupBy(a => a.Category, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var group in grouped)
        {
            // Use the first item's sentiment as the category-level emoji
            var categorySentiment = group.First().Sentiment;
            var emoji = SentimentToEmoji(categorySentiment);

            sb.AppendLine($"{emoji} {group.Key.ToUpperInvariant()}");
            sb.AppendLine();

            foreach (var assessment in group)
            {
                sb.AppendLine($"- **{assessment.Headline}** — {assessment.Summary}");
            }

            sb.AppendLine();
        }

        sb.AppendLine("---");
        sb.AppendLine();

        var moodEmoji = SentimentToEmoji(item.Mood);
        sb.AppendLine($"OVERALL MOOD: {moodEmoji} {item.Summary}");

        return sb.ToString().TrimEnd();
    }

    private static string SentimentToEmoji(MarketSentiment sentiment) => sentiment switch
    {
        MarketSentiment.RiskOff => "🔴",
        MarketSentiment.RiskOn => "🟢",
        MarketSentiment.Mixed => "🟡",
        _ => "🟡"
    };
}
