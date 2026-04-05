using System.Reactive.Concurrency;
using System.Reactive.Linq;
using FikaForecast.Application.DTOs;
using FikaForecast.Application.Interfaces;
using FikaForecast.Domain.ValueObjects;
using NLog;

namespace FikaForecast.Application.Services;

/// <summary>
/// Builds daily batch schedules and executes batch slots by delegating to the comparison service.
/// Owns the <see cref="IScheduler"/> so timer creation is testable with <c>TestScheduler</c>.
/// </summary>
public class BatchSchedulingService : IBatchSchedulingService
{
    private readonly ILogger _logger;
    private readonly IBriefComparisonService _comparisonService;
    private readonly IScheduler _scheduler;

    public BatchSchedulingService(
        ILogger logger,
        IBriefComparisonService comparisonService,
        IScheduler scheduler)
    {
        _logger = logger;
        _comparisonService = comparisonService;
        _scheduler = scheduler;
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
    public IObservable<long> CreateTimer(TimeSpan delay)
    {
        return Observable.Timer(delay, _scheduler);
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

    /// <inheritdoc />
    public TimeSpan CalculateWeeklyDelay(DayOfWeek targetDay, TimeOnly targetTime, DateTime utcNow)
    {
        var todayDay = utcNow.DayOfWeek;
        var daysUntil = ((int)targetDay - (int)todayDay + 7) % 7;

        var targetDateTime = utcNow.Date.AddDays(daysUntil) + targetTime.ToTimeSpan();

        // If it's the same day but the time has passed (or exactly now), skip to next week
        if (targetDateTime <= utcNow)
            targetDateTime = targetDateTime.AddDays(7);

        return targetDateTime - utcNow;
    }
}
