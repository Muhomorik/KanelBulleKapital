using System.Text.Json;
using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using KanelBrief.Core.Agents;
using KanelBrief.Core.Models;
using KanelBrief.Core.Parsers;
using KanelBrief.Core.Serialization;
using Microsoft.Extensions.Logging;

namespace KanelBrief.Functions.Agents.Analyzers;

/// <summary>
/// Azure AI Foundry implementation of <see cref="ISubstitutionChainAnalyzer"/>.
/// Uses the in-memory <c>AsAIAgent</c> extension to run an ephemeral agent.
/// </summary>
public sealed class AzureSubstitutionChainAnalyzer : ISubstitutionChainAnalyzer
{
    private const string ModelId = "gpt-5.4-mini";

    private readonly ILogger<AzureSubstitutionChainAnalyzer> _logger;
    private readonly AIProjectClient _aiProjectClient;
    private readonly JsonSerializerOptions _jsonOptions = KanelJsonOptions.CamelCase;

    public AzureSubstitutionChainAnalyzer(
        ILogger<AzureSubstitutionChainAnalyzer> logger,
        AIProjectClient aiProjectClient)
    {
        _logger = logger;
        _aiProjectClient = aiProjectClient;
    }

    public async Task<SubstitutionChainAnalysisResult> AnalyzeAsync(
        WeeklySummaryRun weeklySummary,
        CancellationToken ct = default)
    {
        var summaryContext = $"Net Mood: {weeklySummary.NetMood}\nSummary: {weeklySummary.MoodSummary}\nThemes:\n{JsonSerializer.Serialize(weeklySummary.Themes, _jsonOptions)}";

        var agent = _aiProjectClient.AsAIAgent(
            model: ModelId,
            name: "SubstitutionChainAnalyzer",
            instructions: @"You are a capital rotation analyst. Based on weekly market themes, identify where capital is flowing from and to.
Identify 2-4 rotation chains based on the sentiment data.

Return ONLY a JSON object with this exact structure:
{
  ""chains"": [
    {
      ""capitalFleeing"": ""Sector losing capital"",
      ""flowsToward"": ""Sector gaining capital"",
      ""mechanism"": ""Why capital is rotating""
    }
  ]
}");

        var prompt = $"Based on this weekly summary, identify capital rotation chains:\n\n{summaryContext}";
        var response = await agent.RunAsync(prompt);
        var json = AgentResponseParser.ExtractJson(response.ToString() ?? string.Empty);
        return JsonSerializer.Deserialize<SubstitutionChainAnalysisResult>(json, _jsonOptions)
            ?? throw new InvalidOperationException("Failed to parse substitution chains response");
    }
}
