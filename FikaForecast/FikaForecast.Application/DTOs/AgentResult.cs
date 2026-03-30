using System.Diagnostics;

namespace FikaForecast.Application.DTOs;

/// <summary>
/// Raw output returned by the agent after a single execution.
/// </summary>
[DebuggerDisplay("In: {InputTokens} Out: {OutputTokens} ({Duration.TotalSeconds:F1}s)")]
public sealed record AgentResult(
    string RawMarkdownOutput,
    int InputTokens,
    int OutputTokens,
    TimeSpan Duration);
