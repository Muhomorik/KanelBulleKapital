namespace KanelBrief.Core.Models;

/// <summary>Result of a Weekly Summary agent run. Stored in the <c>WeeklySummaryRuns</c> Azure Table.</summary>
public class WeeklySummaryRun : AgentRunBase
{
    /// <summary>Discriminator constant — identifies this run shape on the wire and in YAML metadata.</summary>
    public string ReportType { get; set; } = "weekly-summary";

    public DateTimeOffset PeriodStart { get; set; }

    public DateTimeOffset PeriodEnd { get; set; }

    /// <summary>ISO 8601 week tag of <see cref="PeriodStart"/>, e.g. <c>"2026-W17"</c>.</summary>
    public string PeriodIsoWeek { get; set; } = string.Empty;

    public MarketSentiment NetMood { get; set; }

    public string MoodSummary { get; set; } = string.Empty;

    public List<WeeklySummaryTheme> Themes { get; set; } = [];
}
