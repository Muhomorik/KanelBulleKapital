using FikaForecast.Application.DTOs;
using FikaForecast.Domain.ValueObjects;
using FluentResults;

namespace FikaForecast.Application.Interfaces;

/// <summary>
/// Evaluates a news brief report against prompt rules using an LLM as judge.
/// </summary>
public interface IEvaluationAgent
{
    /// <summary>
    /// Sends the report and original prompt rules to the judge model for compliance evaluation.
    /// </summary>
    /// <param name="model">The model to use as the judge.</param>
    /// <param name="evaluationPrompt">Judge instructions (system prompt).</param>
    /// <param name="reportMarkdown">The news brief output to evaluate.</param>
    /// <param name="originalPrompt">The prompt rules the report was supposed to follow.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Result<AgentResult>> EvaluateAsync(
        ModelConfig model,
        AgentPrompt evaluationPrompt,
        string reportMarkdown,
        AgentPrompt originalPrompt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends multiple model reports to the judge for side-by-side comparison.
    /// </summary>
    /// <param name="model">The model to use as the judge.</param>
    /// <param name="comparisonPrompt">Comparison judge instructions (system prompt).</param>
    /// <param name="reports">Labeled reports: model name + markdown output.</param>
    /// <param name="originalPrompt">The prompt rules all reports were supposed to follow.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Result<AgentResult>> CompareAsync(
        ModelConfig model,
        AgentPrompt comparisonPrompt,
        IReadOnlyList<(string ModelName, string ReportMarkdown)> reports,
        AgentPrompt originalPrompt,
        CancellationToken cancellationToken = default);
}
