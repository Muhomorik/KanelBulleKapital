using FikaForecast.Domain.ValueObjects;

namespace FikaForecast.Application.Interfaces;

/// <summary>
/// Provides agent prompts used across the application.
/// Hardcoded for now — will be moved to user settings later.
/// </summary>
public interface IPromptProvider
{
    /// <summary>Returns the News Brief generation prompt.</summary>
    AgentPrompt GetNewsBriefPrompt();

    /// <summary>Returns the LLM-as-judge evaluation prompt for a single report.</summary>
    AgentPrompt GetEvaluationPrompt();

    /// <summary>Returns the LLM-as-judge comparison prompt for multiple model reports.</summary>
    AgentPrompt GetComparisonPrompt();
}
