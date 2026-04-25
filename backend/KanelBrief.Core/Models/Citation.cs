using System.Text.Json.Serialization;

namespace KanelBrief.Core.Models;

/// <summary>
/// URL citation from the agent response's annotation channel, pinning a webpage
/// to a character span in the model output.
/// </summary>
/// <remarks>
/// Returns empty for JSON-mode agents — Foundry only attaches annotations to
/// prose output. See docs/AZURE-DEPLOYMENT.md § Persistent Agent Setup.
/// </remarks>
public sealed record Citation(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("startIndex")] int StartIndex,
    [property: JsonPropertyName("endIndex")] int EndIndex);
