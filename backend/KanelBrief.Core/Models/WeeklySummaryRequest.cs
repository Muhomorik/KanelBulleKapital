namespace KanelBrief.Core.Models;

/// <summary>Input request for the Weekly Summary agent.</summary>
public class WeeklySummaryRequest
{
    public DateTimeOffset PeriodStart { get; set; }
    public DateTimeOffset PeriodEnd { get; set; }
    public List<string> DailyBriefRunIds { get; set; } = [];
}
