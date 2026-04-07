namespace KanelBrief.Core.Models;

/// <summary>Result of a Weekly Summary agent run. Stored in the <c>WeeklySummaryRuns</c> Azure Table.</summary>
public class WeeklySummaryRun : AgentRunBase
{
    public DateTimeOffset WeekStart { get; set; }

    public DateTimeOffset WeekEnd { get; set; }

    public MarketSentiment NetMood { get; set; }

    public string MoodSummary { get; set; } = string.Empty;

    public List<WeeklySummaryTheme> Themes { get; set; } = [];
}
