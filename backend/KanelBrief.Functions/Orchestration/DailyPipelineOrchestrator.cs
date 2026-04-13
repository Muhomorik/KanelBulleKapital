using KanelBrief.Core.Pipelines;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace KanelBrief.Functions.Orchestration;

/// <summary>
/// Timer host for the agent pipelines. Only concern is routing TimerTrigger events
/// to the corresponding pipeline service — no business logic lives here.
/// </summary>
public sealed class DailyPipelineOrchestrator(
    ILogger<DailyPipelineOrchestrator> logger,
    INewsBriefPipeline newsBriefPipeline,
    IWeeklyAggregationPipeline weeklyAggregationPipeline)
{
    /// <summary>Cron schedule: every 4 hours at minute 0 UTC (5 fields: minute hour day month day-of-week). Fires at 00, 04, 08, 12, 16, 20 UTC.</summary>
    /// <remarks>If changed, also update frontend/components/footer.tsx (schedule display).</remarks>
    public const string DAILY_BRIEF_SCHEDULE = "0 */4 * * *";

    /// <summary>Cron schedule: every Thursday at 21 UTC.</summary>
    /// <remarks>If changed, also update frontend/components/footer.tsx (schedule display).</remarks>
    public const string WEEKLY_AGGREGATION_SCHEDULE = "0 21 * * 4";

    /// <summary>Daily timer trigger: delegates to <see cref="INewsBriefPipeline"/>.</summary>
    [Function("DailyNewsBriefTimer")]
    public Task RunDailyNewsBrief(
        [TimerTrigger(DAILY_BRIEF_SCHEDULE)] TimerInfo timer,
        CancellationToken ct)
    {
        if (timer.IsPastDue)
            logger.LogWarning("Daily News Brief execution is behind schedule");

        return newsBriefPipeline.ExecuteAsync(ct);
    }

    /// <summary>Weekly timer trigger: delegates to <see cref="IWeeklyAggregationPipeline"/>.</summary>
    [Function("WeeklyAggregationTimer")]
    public Task RunWeeklyAggregation(
        [TimerTrigger(WEEKLY_AGGREGATION_SCHEDULE)] TimerInfo timer,
        CancellationToken ct)
    {
        if (timer.IsPastDue)
            logger.LogWarning("Weekly aggregation execution is behind schedule");

        return weeklyAggregationPipeline.ExecuteAsync(ct);
    }
}
