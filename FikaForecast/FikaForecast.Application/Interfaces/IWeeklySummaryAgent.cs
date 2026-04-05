using FikaForecast.Application.DTOs;
using FikaForecast.Domain.ValueObjects;
using FluentResults;

namespace FikaForecast.Application.Interfaces;

/// <summary>
/// Executes the Weekly Summary Agent via chat completions.
/// Unlike <see cref="INewsBriefAgent"/>, this agent receives pre-built input text
/// (formatted daily briefs) and does not search the web.
/// </summary>
public interface IWeeklySummaryAgent
{
    /// <summary>
    /// Runs the agent with the given model, prompt, and formatted daily briefs input.
    /// </summary>
    /// <param name="model">Azure AI Foundry model deployment to use.</param>
    /// <param name="prompt">System prompt for the agent.</param>
    /// <param name="inputText">Formatted daily briefs text built from DB data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing raw JSON output with token counts and duration, or error details.</returns>
    Task<Result<AgentResult>> RunAsync(
        ModelConfig model,
        AgentPrompt prompt,
        string inputText,
        CancellationToken cancellationToken = default);
}
