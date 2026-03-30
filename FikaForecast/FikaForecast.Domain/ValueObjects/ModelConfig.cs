using System.Diagnostics;

namespace FikaForecast.Domain.ValueObjects;

/// <summary>
/// Identifies an LLM deployment in the Azure AI Foundry catalog.
/// </summary>
/// <param name="ModelId">Catalog model identifier (e.g. "gpt-5.1-mini").</param>
/// <param name="DeploymentName">Azure AI Foundry deployment name.</param>
/// <param name="DisplayName">Human-readable label for the UI.</param>
[DebuggerDisplay("{DisplayName} ({DeploymentName})")]
public sealed record ModelConfig(
    string ModelId,
    string DeploymentName,
    string DisplayName);
