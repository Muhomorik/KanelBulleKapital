using FikaForecast.Domain.ValueObjects;

namespace FikaForecast.Application.Interfaces;

/// <summary>
/// Provides agent prompts loaded from external files with embedded resource fallback.
/// </summary>
public interface IPromptProvider
{
    /// <summary>Returns the News Brief generation prompt.</summary>
    AgentPrompt GetNewsBriefPrompt();

    /// <summary>Returns the LLM-as-judge evaluation prompt for a single report.</summary>
    AgentPrompt GetEvaluationPrompt();

    /// <summary>Returns the LLM-as-judge comparison prompt for multiple model reports.</summary>
    AgentPrompt GetComparisonPrompt();

    /// <summary>
    /// Clears the in-memory prompt cache, causing prompts to be re-read
    /// from disk on next access. Call after editing prompt files.
    /// </summary>
    void InvalidateCache();
}
