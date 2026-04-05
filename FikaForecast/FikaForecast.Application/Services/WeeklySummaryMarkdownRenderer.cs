using System.Text;
using FikaForecast.Application.DTOs;
using FikaForecast.Domain.Enums;

namespace FikaForecast.Application.Services;

/// <summary>
/// Renders a <see cref="WeeklySummaryParseResult"/> into display markdown with emojis
/// for the WebView2 UI. Groups themes by confidence level.
/// </summary>
public class WeeklySummaryMarkdownRenderer
{
    /// <summary>
    /// Renders the parse result as emoji markdown, grouped by confidence level.
    /// </summary>
    public string Render(
        WeeklySummaryParseResult parseResult,
        DateTimeOffset weekStart,
        DateTimeOffset weekEnd)
    {
        if (parseResult.Themes.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();

        sb.AppendLine($"WEEKLY MARKET SUMMARY — {weekStart:MMMM dd}--{weekEnd:MMMM dd, yyyy}");
        sb.AppendLine();
        sb.AppendLine("---");

        var highThemes = parseResult.Themes.Where(t => t.Confidence == ConfidenceLevel.High).ToList();
        var moderateThemes = parseResult.Themes.Where(t => t.Confidence == ConfidenceLevel.Moderate).ToList();
        var droppedThemes = parseResult.Themes.Where(t => t.Confidence == ConfidenceLevel.Dropped).ToList();

        if (highThemes.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("HIGH CONFIDENCE:");
            sb.AppendLine();
            foreach (var theme in highThemes)
            {
                var emoji = SentimentToEmoji(theme.Sentiment);
                sb.AppendLine($"{emoji} {theme.Category.ToUpperInvariant()}");
                sb.AppendLine($"- {theme.Summary}");
                sb.AppendLine();
            }
        }

        if (moderateThemes.Count > 0)
        {
            sb.AppendLine("MODERATE CONFIDENCE:");
            sb.AppendLine();
            foreach (var theme in moderateThemes)
            {
                var emoji = SentimentToEmoji(theme.Sentiment);
                sb.AppendLine($"{emoji} {theme.Category.ToUpperInvariant()}");
                sb.AppendLine($"- {theme.Summary}");
                sb.AppendLine();
            }
        }

        if (droppedThemes.Count > 0)
        {
            sb.AppendLine("DROPPED:");
            foreach (var theme in droppedThemes)
            {
                sb.AppendLine($"- {theme.Category} — {theme.Summary}");
            }
            sb.AppendLine();
        }

        sb.AppendLine("---");
        sb.AppendLine();

        var moodEmoji = SentimentToEmoji(parseResult.NetMood);
        sb.AppendLine($"NET WEEKLY MOOD: {moodEmoji} {parseResult.MoodSummary}");

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
