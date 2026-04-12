namespace KanelBrief.Core.Pipelines;

/// <summary>
/// Orchestrates the Weekly Aggregation run: fetches the previous week's daily briefs,
/// chains Weekly Summary → Substitution Chain → Opportunity Scan, persists each step.
/// </summary>
public interface IWeeklyAggregationPipeline
{
    Task ExecuteAsync(CancellationToken ct = default);
}
