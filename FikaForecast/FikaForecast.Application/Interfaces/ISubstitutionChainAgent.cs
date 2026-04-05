using FikaForecast.Application.DTOs;
using FikaForecast.Domain.ValueObjects;
using FluentResults;

namespace FikaForecast.Application.Interfaces;

/// <summary>
/// Executes the Substitution Chain Agent via chat completions.
/// Receives pre-built input text (formatted weekly summary themes)
/// and does not search the web.
/// </summary>
public interface ISubstitutionChainAgent
{
    /// <summary>
    /// Runs the agent with the given model, prompt, and formatted weekly summary input.
    /// </summary>
    /// <param name="model">Azure AI Foundry model deployment to use.</param>
    /// <param name="prompt">System prompt for the agent.</param>
    /// <param name="inputText">Formatted weekly summary text built from DB data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing raw JSON output with token counts and duration, or error details.</returns>
    Task<Result<AgentResult>> RunAsync(
        ModelConfig model,
        AgentPrompt prompt,
        string inputText,
        CancellationToken cancellationToken = default);
}
