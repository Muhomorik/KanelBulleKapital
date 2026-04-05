using System.Text;
using FikaForecast.Domain.Entities;
using FikaForecast.Domain.Enums;

namespace FikaForecast.Application.Services;

/// <summary>
/// Formats daily <see cref="NewsBriefRun"/> data into text input for the Weekly Summary Agent.
/// Groups runs by date and renders each day's category assessments with sentiment emojis.
/// </summary>
public class WeeklySummaryInputFormatter
{
    /// <summary>
    /// Formats a list of daily brief runs into a text block for the LLM prompt.
    /// Runs must include eager-loaded Item and Assessments.
    /// </summary>
    public string Format(IReadOnlyList<NewsBriefRun> runs)
    {
        if (runs.Count == 0)
            return string.Empty;

        var orderedRuns = runs
            .Where(r => r.Item is not null && r.Item.Assessments.Count > 0)
            .OrderBy(r => r.Timestamp)
            .ToList();

        if (orderedRuns.Count == 0)
            return string.Empty;

        var weekStart = orderedRuns.First().Timestamp;
        var weekEnd = orderedRuns.Last().Timestamp;

        var sb = new StringBuilder();
        sb.AppendLine($"DAILY BRIEFS — {weekStart:MMMM dd}--{weekEnd:MMMM dd, yyyy}");
        sb.AppendLine();
        sb.AppendLine("---");

        // Group by calendar date
        var byDate = orderedRuns
            .GroupBy(r => r.Timestamp.Date)
            .OrderBy(g => g.Key);

        foreach (var dayGroup in byDate)
        {
            sb.AppendLine();
            sb.AppendLine($"{dayGroup.Key:dddd MMMM dd}:".ToUpperInvariant());

            // Take the latest run per day (in case of multiple runs)
            var latestRun = dayGroup.OrderByDescending(r => r.Timestamp).First();
            var item = latestRun.Item!;

            foreach (var assessment in item.Assessments)
            {
                var emoji = SentimentToEmoji(assessment.Sentiment);
                sb.AppendLine($"{emoji} {assessment.Category.ToUpperInvariant()}: " +
                              $"{assessment.Headline}. {assessment.Summary}");
            }
        }

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
