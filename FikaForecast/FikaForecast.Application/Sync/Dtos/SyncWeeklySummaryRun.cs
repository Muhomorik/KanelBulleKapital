namespace FikaForecast.Application.Sync.Dtos;

/// <summary>
/// Wire DTO mirroring backend <c>KanelBrief.Core.Models.WeeklySummaryRun</c>.
/// </summary>
public sealed class SyncWeeklySummaryRun
{
    // AgentRunBase fields
    public string RunDate { get; set; } = "";
    public string RunId { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public string ModelId { get; set; } = "";
    public string Status { get; set; } = "";
    public double DurationSeconds { get; set; }
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int TotalTokens { get; set; }

    // WeeklySummaryRun-specific
    public DateTimeOffset WeekStart { get; set; }
    public DateTimeOffset WeekEnd { get; set; }
    public string NetMood { get; set; } = "";
    public string MoodSummary { get; set; } = "";
    public List<SyncWeeklySummaryTheme> Themes { get; set; } = [];
}

public sealed class SyncWeeklySummaryTheme
{
    public string Category { get; set; } = "";
    public string Summary { get; set; } = "";
    public string Confidence { get; set; } = "";
    public string Sentiment { get; set; } = "";
}
