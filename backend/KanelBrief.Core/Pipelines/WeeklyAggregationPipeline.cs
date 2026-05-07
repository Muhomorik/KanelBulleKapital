using KanelBrief.Core.Agents;
using KanelBrief.Core.Models;
using KanelBrief.Core.Parsers;
using KanelBrief.Core.Repositories;
using KanelBrief.Core.Time;
using Microsoft.Extensions.Logging;

namespace KanelBrief.Core.Pipelines;

/// <inheritdoc />
public sealed class WeeklyAggregationPipeline : IWeeklyAggregationPipeline
{
    private const string ModelId = "gpt-5.4-mini";

    private readonly ILogger<WeeklyAggregationPipeline> _logger;
    private readonly IAgentRunRepository _repository;
    private readonly IWeeklySummaryAnalyzer _weeklySummaryAnalyzer;
    private readonly ISubstitutionChainAnalyzer _substitutionChainAnalyzer;
    private readonly IOpportunityScanAnalyzer _opportunityScanAnalyzer;
    private readonly TimeProvider _timeProvider;

    public WeeklyAggregationPipeline(
        ILogger<WeeklyAggregationPipeline> logger,
        IAgentRunRepository repository,
        IWeeklySummaryAnalyzer weeklySummaryAnalyzer,
        ISubstitutionChainAnalyzer substitutionChainAnalyzer,
        IOpportunityScanAnalyzer opportunityScanAnalyzer,
        TimeProvider timeProvider)
    {
        _logger = logger;
        _repository = repository;
        _weeklySummaryAnalyzer = weeklySummaryAnalyzer;
        _substitutionChainAnalyzer = substitutionChainAnalyzer;
        _opportunityScanAnalyzer = opportunityScanAnalyzer;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Executes the weekly aggregation chain: Weekly Summary → Substitution Chain → Opportunity Scan.
    /// Any step that throws hard-aborts the pipeline — downstream steps do NOT run and nothing
    /// is persisted beyond successfully-completed steps. Exceptions are logged with context
    /// and rethrown so App Insights records a failed invocation.
    /// </summary>
    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Weekly aggregation pipeline starting");

        var (prevMonday, prevSunday) = CalculateWeekBoundaries(_timeProvider.GetUtcNow());

        _logger.LogInformation("Processing week from {PeriodStart} to {PeriodEnd}",
            prevMonday.ToString("yyyy-MM-dd"), prevSunday.ToString("yyyy-MM-dd"));

        var dailyBriefs = new List<NewsBriefRun>();
        for (var date = prevMonday; date < prevSunday; date = date.AddDays(1))
        {
            var briefsForDate = await _repository.GetNewsBriefRunsByDateAsync(date.ToString("yyyy-MM-dd"));
            dailyBriefs.AddRange(briefsForDate);
        }

        _logger.LogInformation("Retrieved {BriefCount} news briefs from the week", dailyBriefs.Count);

        if (dailyBriefs.Count == 0)
        {
            _logger.LogWarning("No daily briefs found for the week, skipping aggregation");
            return;
        }

        try
        {
            var weeklySummaryRun = await RunWeeklySummaryAsync(prevMonday, prevSunday, dailyBriefs, ct);
            var substitutionChainRun = await RunSubstitutionChainAsync(weeklySummaryRun, ct);
            await RunOpportunityScanAsync(substitutionChainRun, ct);

            _logger.LogInformation("Weekly aggregation pipeline completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Weekly aggregation pipeline failed for week {PeriodStart} → {PeriodEnd}",
                prevMonday.ToString("yyyy-MM-dd"), prevSunday.ToString("yyyy-MM-dd"));
            throw;
        }
    }

    private async Task<WeeklySummaryRun> RunWeeklySummaryAsync(
        DateTime weekStart, DateTime weekEnd, IReadOnlyList<NewsBriefRun> dailyBriefs, CancellationToken ct)
    {
        var startTime = _timeProvider.GetUtcNow();
        var analysis = await _weeklySummaryAnalyzer.AnalyzeAsync(weekStart, weekEnd, dailyBriefs, ct);

        var periodStart = new DateTimeOffset(weekStart, TimeSpan.Zero);
        var periodEnd = new DateTimeOffset(weekEnd, TimeSpan.Zero);
        var run = new WeeklySummaryRun
        {
            RunDate = startTime.ToString("yyyy-MM-dd"),
            RunId = WeeklySummaryRunId.NewId(),
            CreatedAt = startTime,
            ModelId = ModelId,
            Status = RunStatus.Success,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            PeriodIsoWeek = IsoWeek.Format(periodStart),
            NetMood = AgentResponseParser.ParseSentiment(analysis.Mood),
            MoodSummary = analysis.Summary,
            Themes = analysis.Themes,
            DurationSeconds = (_timeProvider.GetUtcNow() - startTime).TotalSeconds,
            InputTokens = analysis.InputTokens,
            OutputTokens = analysis.OutputTokens,
            TotalTokens = analysis.TotalTokens
        };

        await _repository.SaveWeeklySummaryRunAsync(run);
        _logger.LogInformation(
            "Weekly Summary saved: {RunId} (Mood={Mood}, Themes={Count})",
            run.RunId, run.NetMood, run.Themes.Count);
        return run;
    }

    private async Task<SubstitutionChainRun> RunSubstitutionChainAsync(WeeklySummaryRun weeklySummary, CancellationToken ct)
    {
        var startTime = _timeProvider.GetUtcNow();
        var analysis = await _substitutionChainAnalyzer.AnalyzeAsync(weeklySummary, ct);

        var run = new SubstitutionChainRun
        {
            RunDate = startTime.ToString("yyyy-MM-dd"),
            RunId = SubstitutionChainRunId.NewId(),
            CreatedAt = startTime,
            ModelId = ModelId,
            Status = RunStatus.Success,
            WeeklySummaryRunId = weeklySummary.RunId,
            Chains = analysis.Chains,
            DurationSeconds = (_timeProvider.GetUtcNow() - startTime).TotalSeconds,
            InputTokens = analysis.InputTokens,
            OutputTokens = analysis.OutputTokens,
            TotalTokens = analysis.TotalTokens
        };

        await _repository.SaveSubstitutionChainRunAsync(run);
        _logger.LogInformation("Substitution Chain saved: {RunId} ({ChainCount} chains)",
            run.RunId, run.Chains.Count);
        return run;
    }

    private async Task RunOpportunityScanAsync(SubstitutionChainRun substitutionChain, CancellationToken ct)
    {
        var startTime = _timeProvider.GetUtcNow();
        var analysis = await _opportunityScanAnalyzer.AnalyzeAsync(substitutionChain, ct);

        var run = new OpportunityScanRun
        {
            RunDate = startTime.ToString("yyyy-MM-dd"),
            RunId = OpportunityScanRunId.NewId(),
            CreatedAt = startTime,
            ModelId = ModelId,
            Status = RunStatus.Success,
            SubstitutionChainRunId = substitutionChain.RunId,
            Targets = analysis.Targets,
            DurationSeconds = (_timeProvider.GetUtcNow() - startTime).TotalSeconds,
            InputTokens = analysis.InputTokens,
            OutputTokens = analysis.OutputTokens,
            TotalTokens = analysis.TotalTokens
        };

        await _repository.SaveOpportunityScanRunAsync(run);
        _logger.LogInformation("Opportunity Scan saved: {RunId} ({TargetCount} targets)",
            run.RunId, run.Targets.Count);
    }

    /// <summary>
    /// Calculates the previous week's Monday-to-Sunday boundaries relative to the given timestamp.
    /// The returned <c>prevSunday</c> is the exclusive upper bound (i.e. the Monday after the week's Sunday).
    /// </summary>
    internal static (DateTime prevMonday, DateTime prevSunday) CalculateWeekBoundaries(DateTimeOffset now)
    {
        var daysFromMonday = ((int)now.DayOfWeek + 6) % 7; // Monday = 0
        var thisMonday = now.AddDays(-daysFromMonday).Date;
        var prevMonday = thisMonday.AddDays(-7);
        var prevSunday = thisMonday;
        return (prevMonday, prevSunday);
    }
}
