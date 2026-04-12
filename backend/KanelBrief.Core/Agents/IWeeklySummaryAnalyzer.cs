using KanelBrief.Core.Models;

namespace KanelBrief.Core.Agents;

/// <summary>
/// Domain port for Weekly Summary analysis. Takes a week's daily briefs and returns aggregated themes.
/// </summary>
public interface IWeeklySummaryAnalyzer
{
    Task<WeeklySummaryAnalysisResult> AnalyzeAsync(
        DateTime weekStart,
        DateTime weekEnd,
        IReadOnlyList<NewsBriefRun> dailyBriefs,
        CancellationToken ct = default);
}
