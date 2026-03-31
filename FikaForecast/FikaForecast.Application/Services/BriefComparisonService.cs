using FikaForecast.Domain.Entities;
using FikaForecast.Domain.ValueObjects;
using FluentResults;

namespace FikaForecast.Application.Services;

/// <summary>
/// Runs the same News Brief Agent prompt across multiple models in parallel for side-by-side comparison.
/// </summary>
public class BriefComparisonService
{
    private readonly NewsBriefOrchestrator _orchestrator;

    public BriefComparisonService(NewsBriefOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    /// <summary>
    /// Executes the brief for each model concurrently. Each model can independently succeed or fail.
    /// </summary>
    public async Task<IReadOnlyList<Result<NewsBriefRun>>> CompareAsync(
        IReadOnlyList<ModelConfig> models,
        AgentPrompt prompt,
        CancellationToken cancellationToken = default)
    {
        var tasks = models.Select(model =>
            _orchestrator.RunBriefAsync(model, prompt, cancellationToken));

        var results = await Task.WhenAll(tasks);
        return results;
    }
}
