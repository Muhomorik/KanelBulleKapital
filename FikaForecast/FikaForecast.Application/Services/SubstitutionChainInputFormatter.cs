using System.Text;
using FikaForecast.Domain.Entities;
using FikaForecast.Domain.Enums;

namespace FikaForecast.Application.Services;

/// <summary>
/// Formats a <see cref="WeeklySummaryRun"/> and its themes into text input
/// for the Substitution Chain Agent. Groups themes by confidence level.
/// </summary>
public class SubstitutionChainInputFormatter
{
    /// <summary>
    /// Formats the weekly summary run and its themes into a text block for the LLM prompt.
    /// The run must include eager-loaded Themes.
    /// </summary>
    public string Format(WeeklySummaryRun summaryRun)
    {
        if (summaryRun.Themes.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();

        sb.AppendLine($"WEEKLY MARKET SUMMARY — Week of {summaryRun.WeekStart:MMMM dd}--{summaryRun.WeekEnd:MMMM dd, yyyy}");
        sb.AppendLine();
        sb.AppendLine("---");

        var highThemes = summaryRun.Themes.Where(t => t.Confidence == ConfidenceLevel.High).ToList();
        var moderateThemes = summaryRun.Themes.Where(t => t.Confidence == ConfidenceLevel.Moderate).ToList();
        var droppedThemes = summaryRun.Themes.Where(t => t.Confidence == ConfidenceLevel.Dropped).ToList();

        if (highThemes.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine($"HIGH CONFIDENCE ({highThemes.Count} themes):");
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
            sb.AppendLine($"MODERATE CONFIDENCE ({moderateThemes.Count} themes):");
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
            sb.AppendLine("DROPPED (low confidence / inconsistent):");
            foreach (var theme in droppedThemes)
            {
                sb.AppendLine($"- {theme.Category} — {theme.Summary}");
            }
            sb.AppendLine();
        }

        sb.AppendLine("---");
        sb.AppendLine();

        var moodEmoji = SentimentToEmoji(summaryRun.NetMood);
        sb.AppendLine($"NET WEEKLY MOOD: {moodEmoji} {summaryRun.MoodSummary}");

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
