using System.Diagnostics;

namespace FikaForecast.Domain.ValueObjects;

/// <summary>
/// A named system prompt for the News Brief Agent. Loaded from config to allow
/// prompt customization and A/B testing without code changes.
/// </summary>
/// <param name="Name">Display name (e.g. "News Brief - Default").</param>
/// <param name="SystemPrompt">The full system prompt text sent to the LLM.</param>
[DebuggerDisplay("{Name}")]
public sealed record AgentPrompt(
    string Name,
    string SystemPrompt);
