using FikaForecast.Domain.Entities;
using FikaForecast.Domain.ValueObjects;

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
    /// Executes the brief for each model concurrently and returns all results.
    /// </summary>
    /// <param name="models">Models to compare (2+).</param>
    /// <param name="prompt">System prompt — identical for all models.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>One <see cref="NewsBriefRun"/> per model, all persisted.</returns>
    public async Task<IReadOnlyList<NewsBriefRun>> CompareAsync(
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
