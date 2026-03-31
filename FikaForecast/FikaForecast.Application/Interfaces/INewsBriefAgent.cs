using FikaForecast.Application.DTOs;
using FikaForecast.Domain.ValueObjects;
using FluentResults;

namespace FikaForecast.Application.Interfaces;

/// <summary>
/// Executes the News Brief Agent against a specific model deployment.
/// </summary>
/// <remarks>
/// Implemented by <c>AgentFrameworkNewsBriefAgent</c> in the Infrastructure layer
/// using Microsoft Agent Framework + Azure AI Foundry.
/// </remarks>
public interface INewsBriefAgent
{
    /// <summary>
    /// Runs the agent with the given model and prompt, returning raw output and token usage.
    /// </summary>
    /// <param name="model">Azure AI Foundry model deployment to use.</param>
    /// <param name="prompt">System prompt for the agent.</param>
    /// <param name="cancellationToken">Cancellation token. Honored at HTTP boundaries.</param>
    /// <returns>Result containing raw markdown output with token counts and duration, or error details.</returns>
    Task<Result<AgentResult>> RunAsync(ModelConfig model, AgentPrompt prompt, CancellationToken cancellationToken = default);
}
