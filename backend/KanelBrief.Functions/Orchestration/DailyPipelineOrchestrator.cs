using System.Text.Json;
using KanelBrief.Core.Models;
using KanelBrief.Core.Repositories;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace KanelBrief.Functions.Orchestration;

/// <summary>
/// Daily pipeline orchestrator: coordinates the sequence of agent runs.
/// Triggers News Brief daily; triggers aggregations weekly.
/// </summary>
public class DailyPipelineOrchestrator
{
    private readonly ILogger<DailyPipelineOrchestrator> _logger;
    private readonly IAgentRunRepository _repository;

    public DailyPipelineOrchestrator(
        ILogger<DailyPipelineOrchestrator> logger,
        IAgentRunRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    /// <summary>Daily timer trigger: executes the News Brief agent every morning at 8 UTC.</summary>
    [Function("DailyNewsBriefTimer")]
    public Task RunDailyNewsBrief(
        [TimerTrigger("0 8 * * *")] TimerInfo timer)
    {
        try
        {
            _logger.LogInformation("Daily News Brief pipeline starting at {Time}", timer.ScheduleStatus?.Last);

            // TODO: Call News Brief agent via HTTP or direct invocation
            // For now, log that it would run
            _logger.LogInformation("Would trigger News Brief agent");

            if (timer.IsPastDue)
            {
                _logger.LogWarning("Daily News Brief execution is behind schedule");
            }

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Daily News Brief pipeline failed");
            throw;
        }
    }

    /// <summary>Weekly timer trigger: aggregates the week's briefs into themes (runs Monday morning).</summary>
    [Function("WeeklyAggregationTimer")]
    public async Task RunWeeklyAggregation(
        [TimerTrigger("0 9 * * 1")] TimerInfo timer)
    {
        try
        {
            _logger.LogInformation("Weekly aggregation pipeline starting at {Time}", timer.ScheduleStatus?.Last);

            // Calculate week boundaries (previous week)
            var now = DateTime.UtcNow;
            var dayOfWeek = (int)now.DayOfWeek;
            var weekStart = now.AddDays(-(dayOfWeek + 6) % 7).Date;
            var weekEnd = weekStart.AddDays(7).Date;

            _logger.LogInformation("Processing week from {WeekStart} to {WeekEnd}", weekStart, weekEnd);

            // Fetch all news briefs from the previous week
            var dailyBriefs = new List<NewsBriefRun>();
            for (var date = weekStart; date < weekEnd; date = date.AddDays(1))
            {
                var briefsForDate = await _repository.GetNewsBriefRunsByDateAsync(date.ToString("yyyy-MM-dd"));
                dailyBriefs.AddRange(briefsForDate);
            }

            _logger.LogInformation("Retrieved {BriefCount} news briefs from the week", dailyBriefs.Count);

            // TODO: Call Weekly Summary agent with the briefs
            // Then chain: Substitution Chain → Opportunity Scan
            _logger.LogInformation("Would trigger Weekly Summary, Substitution Chain, and Opportunity Scan agents");

            if (timer.IsPastDue)
            {
                _logger.LogWarning("Weekly aggregation execution is behind schedule");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Weekly aggregation pipeline failed");
            throw;
        }
    }
}
