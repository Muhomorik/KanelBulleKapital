using FikaForecast.Application.DTOs;
using FikaForecast.Application.Interfaces;
using FikaForecast.Domain.Entities;
using FikaForecast.Domain.ValueObjects;
using FluentResults;

namespace FikaForecast.Application.Services;

/// <summary>
/// Runs the same News Brief Agent prompt across multiple models for side-by-side comparison.
/// </summary>
public class BriefComparisonService : IBriefComparisonService
{
    private readonly NewsBriefOrchestrator _orchestrator;

    public BriefComparisonService(NewsBriefOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public async Task<IReadOnlyList<Result<NewsBriefRun>>> CompareSequentiallyAsync(
        IReadOnlyList<ModelConfig> models,
        AgentPrompt prompt,
        IProgress<BatchProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<Result<NewsBriefRun>>(models.Count);

        for (var i = 0; i < models.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            progress?.Report(new BatchProgress(i, models.Count, models[i].DisplayName));

            var result = await _orchestrator.RunBriefAsync(models[i], prompt, cancellationToken);
            results.Add(result);
        }

        progress?.Report(new BatchProgress(models.Count, models.Count, null));
        return results;
    }
}
