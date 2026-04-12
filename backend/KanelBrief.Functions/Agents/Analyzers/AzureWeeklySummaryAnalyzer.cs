using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using KanelBrief.Core.Agents;
using KanelBrief.Core.Models;
using KanelBrief.Core.Parsers;
using Microsoft.Extensions.Logging;

namespace KanelBrief.Functions.Agents.Analyzers;

/// <summary>
/// Azure AI Foundry implementation of <see cref="IWeeklySummaryAnalyzer"/>.
/// Uses the in-memory <c>AsAIAgent</c> extension to run an ephemeral agent.
/// </summary>
public sealed class AzureWeeklySummaryAnalyzer : IWeeklySummaryAnalyzer
{
    private const string ModelId = "gpt-5.4-mini";

    private readonly ILogger<AzureWeeklySummaryAnalyzer> _logger;
    private readonly AIProjectClient _aiProjectClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public AzureWeeklySummaryAnalyzer(
        ILogger<AzureWeeklySummaryAnalyzer> logger,
        AIProjectClient aiProjectClient)
    {
        _logger = logger;
        _aiProjectClient = aiProjectClient;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    public async Task<WeeklySummaryAnalysisResult> AnalyzeAsync(
        DateTime weekStart,
        DateTime weekEnd,
        IReadOnlyList<NewsBriefRun> dailyBriefs,
        CancellationToken ct = default)
    {
        var briefsContext = string.Join("\n\n", dailyBriefs.Select(b =>
            $"[{b.RunDate}] Mood: {b.Mood}\nSummary: {b.Summary}\nAssessments: {JsonSerializer.Serialize(b.Assessments, _jsonOptions)}"));

        var agent = _aiProjectClient.AsAIAgent(
            model: ModelId,
            name: "WeeklySummaryAnalyzer",
            instructions: @"You are a financial market analyst. Analyze a week of daily market briefs and:
1. Determine the net market mood for the week (RiskOn, RiskOff, or Mixed)
2. Write a 1-2 sentence weekly summary
3. Identify 2-3 key themes that emerged

Return ONLY a JSON object with this exact structure:
{
  ""mood"": ""RiskOn|RiskOff|Mixed"",
  ""summary"": ""Weekly assessment"",
  ""themes"": [
    {
      ""category"": ""Theme name"",
      ""summary"": ""Description"",
      ""confidence"": ""High|Medium|Low"",
      ""sentiment"": ""RiskOn|RiskOff|Mixed""
    }
  ]
}");

        var prompt = $"Analyze this week's market briefs ({weekStart:yyyy-MM-dd} to {weekEnd:yyyy-MM-dd}):\n\n{briefsContext}";
        var response = await agent.RunAsync(prompt);
        var json = AgentResponseParser.ExtractJson(response.ToString() ?? string.Empty);
        return JsonSerializer.Deserialize<WeeklySummaryAnalysisResult>(json, _jsonOptions)
            ?? throw new InvalidOperationException("Failed to parse weekly summary response");
    }
}
