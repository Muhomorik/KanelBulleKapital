using FikaForecast.Application.DTOs;
using FikaForecast.Application.Interfaces;
using FikaForecast.Domain.ValueObjects;
using NLog;

namespace FikaForecast.Application.Services;

/// <summary>
/// Builds daily batch schedules and executes batch slots by delegating to the comparison service.
/// </summary>
public class BatchSchedulingService : IBatchSchedulingService
{
    private readonly ILogger _logger;
    private readonly IBriefComparisonService _comparisonService;

    public BatchSchedulingService(ILogger logger, IBriefComparisonService comparisonService)
    {
        _logger = logger;
        _comparisonService = comparisonService;
    }

    /// <inheritdoc />
    public IReadOnlyList<PlannedTimeSlot> BuildDaySlots(TimeSpan interval)
    {
        var now = TimeOnly.FromDateTime(DateTime.Now);
        var slots = new List<PlannedTimeSlot>();

        for (var hour = 0; hour < 24; hour += (int)interval.TotalHours)
        {
            var time = new TimeOnly(hour, 0);
            slots.Add(new PlannedTimeSlot(time, IsPast: time < now));
        }

        return slots;
    }

    /// <inheritdoc />
    public async Task<BatchSlotResult> ExecuteSlotAsync(
        IReadOnlyList<ModelConfig> models,
        AgentPrompt prompt,
        IProgress<BatchProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        _logger.Info("Batch slot execution started — {0} models", models.Count);
        var startTime = DateTime.Now;

        var results = await _comparisonService.CompareSequentiallyAsync(
            models, prompt, progress, cancellationToken);

        var successes = results.Count(r => r.IsSuccess);
        var failures = results.Count(r => r.IsFailed);
        var totalTokens = results.Where(r => r.IsSuccess).Sum(r => r.Value.TotalTokens);
        var elapsed = DateTime.Now - startTime;

        _logger.Info("Batch slot execution complete — {0}/{1} succeeded, {2} tokens",
            successes, models.Count, totalTokens);

        return new BatchSlotResult(successes, failures, totalTokens, elapsed);
    }
}
