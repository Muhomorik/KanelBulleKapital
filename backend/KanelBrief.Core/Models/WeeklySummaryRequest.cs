namespace KanelBrief.Core.Models;

/// <summary>Input request for the Weekly Summary agent.</summary>
public class WeeklySummaryRequest
{
    public DateTimeOffset WeekStart { get; set; }
    public DateTimeOffset WeekEnd { get; set; }
    public List<string> DailyBriefRunIds { get; set; } = [];
}
