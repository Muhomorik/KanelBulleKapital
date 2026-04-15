using KanelBrief.Core.Agents;
using KanelBrief.Core.Models;
using KanelBrief.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace KanelBrief.Core.Pipelines;

/// <inheritdoc />
public sealed class NewsBriefPipeline : INewsBriefPipeline
{
    private const string ModelId = "gpt-5.4-mini";

    private readonly ILogger<NewsBriefPipeline> _logger;
    private readonly IAgentRunRepository _repository;
    private readonly INewsBriefAnalyzer _analyzer;
    private readonly TimeProvider _timeProvider;

    public NewsBriefPipeline(
        ILogger<NewsBriefPipeline> logger,
        IAgentRunRepository repository,
        INewsBriefAnalyzer analyzer,
        TimeProvider timeProvider)
    {
        _logger = logger;
        _repository = repository;
        _analyzer = analyzer;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Executes the daily News Brief: calls the analyzer, builds the run, persists it.
    /// On failure, logs with context and rethrows — nothing is saved. Relies on App Insights
    /// to capture the exception via the worker-service telemetry wiring.
    /// </summary>
    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Daily News Brief pipeline starting");

        var startTime = _timeProvider.GetUtcNow();
        try
        {
            var analysis = await _analyzer.AnalyzeAsync(startTime, ct);

            var run = new NewsBriefRun
            {
                RunDate = startTime.ToString("yyyy-MM-dd"),
                RunId = Guid.NewGuid().ToString(),
                CreatedAt = startTime,
                ModelId = ModelId,
                DeploymentName = ModelId,
                Status = RunStatus.Success,
                Mood = analysis.Mood,
                Summary = analysis.Summary,
                Assessments = analysis.Assessments,
                DurationSeconds = (_timeProvider.GetUtcNow() - startTime).TotalSeconds,
                InputTokens = analysis.InputTokens,
                OutputTokens = analysis.OutputTokens,
                TotalTokens = analysis.TotalTokens
            };

            await _repository.SaveNewsBriefRunAsync(run);
            _logger.LogInformation(
                "Daily News Brief run saved: {RunId} (Mood={Mood}, Assessments={Count})",
                run.RunId, run.Mood, run.Assessments.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Daily News Brief pipeline failed for {RunDate} after {Elapsed}s",
                startTime.ToString("yyyy-MM-dd"),
                (_timeProvider.GetUtcNow() - startTime).TotalSeconds);
            throw;
        }
    }
}
