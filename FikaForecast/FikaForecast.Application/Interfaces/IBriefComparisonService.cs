using FikaForecast.Application.DTOs;
using FikaForecast.Domain.Entities;
using FikaForecast.Domain.ValueObjects;
using FluentResults;

namespace FikaForecast.Application.Interfaces;

/// <summary>
/// Runs the same News Brief Agent prompt across multiple models for comparison.
/// </summary>
public interface IBriefComparisonService
{
    /// <summary>
    /// Executes the brief for each model concurrently. Each model can independently succeed or fail.
    /// </summary>
    Task<IReadOnlyList<Result<NewsBriefRun>>> CompareAsync(
        IReadOnlyList<ModelConfig> models,
        AgentPrompt prompt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the brief for each model one at a time, reporting progress after each completes.
    /// Used by the batch scheduler to avoid overloading the API with concurrent calls.
    /// </summary>
    Task<IReadOnlyList<Result<NewsBriefRun>>> CompareSequentiallyAsync(
        IReadOnlyList<ModelConfig> models,
        AgentPrompt prompt,
        IProgress<BatchProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
