using System.Text.Json;
using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using KanelBrief.Core.Agents;
using KanelBrief.Core.Models;
using KanelBrief.Core.Parsers;
using KanelBrief.Core.Serialization;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace KanelBrief.Functions.Agents.Analyzers;

/// <summary>
/// Azure AI Foundry implementation of <see cref="IOpportunityScanAnalyzer"/>.
/// Uses the in-memory <c>AsAIAgent</c> extension to run an ephemeral agent.
/// </summary>
public sealed class AzureOpportunityScanAnalyzer : IOpportunityScanAnalyzer
{
    private const string ModelId = "gpt-5.4-mini";
    private const int OutputTokenCap = 2000;

    private readonly ILogger<AzureOpportunityScanAnalyzer> _logger;
    private readonly AIProjectClient _aiProjectClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public AzureOpportunityScanAnalyzer(
        ILogger<AzureOpportunityScanAnalyzer> logger,
        AIProjectClient aiProjectClient)
    {
        _logger = logger;
        _aiProjectClient = aiProjectClient;
        _jsonOptions = KanelJsonOptions.CamelCase;
    }

    public async Task<OpportunityScanAnalysisResult> AnalyzeAsync(
        SubstitutionChainRun substitutionChain,
        CancellationToken ct = default)
    {
        var chainsContext = JsonSerializer.Serialize(substitutionChain.Chains, _jsonOptions);

        var agent = _aiProjectClient.AsAIAgent(
            model: ModelId,
            name: "OpportunityScanAnalyzer",
            instructions: @"You are an investment analyst. Evaluate capital rotation opportunities and identify actionable targets.
Identify 2-4 opportunities with varying signal strengths.

Return ONLY a JSON object with this exact structure:
{
  ""targets"": [
    {
      ""category"": ""Asset or sector"",
      ""signalStrength"": ""Strong|Moderate|Weak"",
      ""rationale"": ""Why this is an opportunity"",
      ""riskCaveat"": ""Key risks to watch""
    }
  ]
}");

        var prompt = $"Based on these capital rotation chains, identify investment opportunities:\n\n{chainsContext}";
        var runOptions = new ChatClientAgentRunOptions(new ChatOptions { MaxOutputTokens = OutputTokenCap });
        var response = await agent.RunAsync(prompt, options: runOptions);
        var json = AgentResponseParser.ExtractJson(response.Text ?? string.Empty);
        var result = JsonSerializer.Deserialize<OpportunityScanAnalysisResult>(json, _jsonOptions)
            ?? throw new InvalidOperationException("Failed to parse opportunities response");

        var usage = response.Usage;
        if (usage is not null)
        {
            result.InputTokens = (int)(usage.InputTokenCount ?? 0);
            result.OutputTokens = (int)(usage.OutputTokenCount ?? 0);
            result.TotalTokens = (int)(usage.TotalTokenCount ?? 0);
        }
        return result;
    }
}
