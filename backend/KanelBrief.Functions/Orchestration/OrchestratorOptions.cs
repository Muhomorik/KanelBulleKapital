using Azure.Identity;

namespace KanelBrief.Functions.Orchestration;

/// <summary>
/// Configuration for the DailyPipelineOrchestrator.
/// Wraps values that can't be injected as bare types (Uri, DefaultAzureCredential, string).
/// </summary>
public class OrchestratorOptions
{
    public required Uri FoundryEndpoint { get; init; }
    public required DefaultAzureCredential Credential { get; init; }
    public string? BingConnectionName { get; init; }
}
