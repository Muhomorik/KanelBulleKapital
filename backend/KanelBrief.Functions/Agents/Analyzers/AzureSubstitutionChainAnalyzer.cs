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
/// Azure AI Foundry implementation of <see cref="ISubstitutionChainAnalyzer"/>.
/// Uses the in-memory <c>AsAIAgent</c> extension to run an ephemeral agent.
/// </summary>
public sealed class AzureSubstitutionChainAnalyzer : ISubstitutionChainAnalyzer
{
    private const string ModelId = "gpt-5.4-mini";
    private const int OutputTokenCap = 2000;

    private readonly ILogger<AzureSubstitutionChainAnalyzer> _logger;
    private readonly AIProjectClient _aiProjectClient;
    private readonly IPromptProvider _promptProvider;
    private readonly JsonSerializerOptions _jsonOptions = KanelJsonOptions.CamelCase;

    public AzureSubstitutionChainAnalyzer(
        ILogger<AzureSubstitutionChainAnalyzer> logger,
        AIProjectClient aiProjectClient,
        IPromptProvider promptProvider)
    {
        _logger = logger;
        _aiProjectClient = aiProjectClient;
        _promptProvider = promptProvider;
    }

    public async Task<SubstitutionChainAnalysisResult> AnalyzeAsync(
        WeeklySummaryRun weeklySummary,
        CancellationToken ct = default)
    {
        var summaryContext = $"Net Mood: {weeklySummary.NetMood}\nSummary: {weeklySummary.MoodSummary}\nThemes:\n{JsonSerializer.Serialize(weeklySummary.Themes, _jsonOptions)}";

        var promptDef = _promptProvider.GetSubstitutionChainPrompt();
        var agent = _aiProjectClient.AsAIAgent(
            model: ModelId,
            name: "SubstitutionChainAnalyzer",
            instructions: promptDef.SystemPrompt);

        var prompt = $"Based on this weekly summary, identify capital rotation chains:\n\n{summaryContext}";
        var runOptions = new ChatClientAgentRunOptions(new ChatOptions { MaxOutputTokens = OutputTokenCap });
        var response = await agent.RunAsync(prompt, options: runOptions);
        var json = AgentResponseParser.ExtractJson(response.Text ?? string.Empty);
        var result = JsonSerializer.Deserialize<SubstitutionChainAnalysisResult>(json, _jsonOptions)
            ?? throw new InvalidOperationException("Failed to parse substitution chains response");

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
