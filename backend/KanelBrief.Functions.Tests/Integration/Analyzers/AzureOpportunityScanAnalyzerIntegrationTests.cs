using Azure.AI.Projects;
using Azure.Identity;
using KanelBrief.Core.Models;
using KanelBrief.Functions.Agents.Analyzers;
using KanelBrief.Functions.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;

namespace KanelBrief.Functions.Tests.Integration.Analyzers;

/// <summary>
/// Hits live Azure AI Foundry. Requires FOUNDRY_PROJECT_ENDPOINT in user secrets and <c>az login</c>.
/// Run manually with: <c>dotnet test --filter "TestCategory=Integration"</c>.
/// </summary>
[TestFixture]
[Explicit("Hits live Azure AI Foundry — run manually, requires user secrets.")]
[Category("Integration")]
public class AzureOpportunityScanAnalyzerIntegrationTests
{
    private AzureOpportunityScanAnalyzer _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var config = IntegrationConfig.Load();
        var endpoint = IntegrationConfig.RequireFoundryEndpoint(config);
        var aiProjectClient = new AIProjectClient(endpoint, new DefaultAzureCredential());

        _sut = new AzureOpportunityScanAnalyzer(
            NullLogger<AzureOpportunityScanAnalyzer>.Instance,
            aiProjectClient,
            new EmbeddedPromptProvider());
    }

    [Test]
    public async Task AnalyzeAsync_AgainstLiveFoundry_ReturnsParsableResult()
    {
        var substitutionChain = new SubstitutionChainRun
        {
            RunId = Guid.NewGuid().ToString(),
            RunDate = "2026-04-06",
            WeeklySummaryRunId = Guid.NewGuid().ToString(),
            Chains =
            [
                new RotationChain { CapitalFleeing = "High-beta tech", FlowsToward = "Utilities", Mechanism = "Yield-sensitive names getting defensive bid" },
                new RotationChain { CapitalFleeing = "Consumer Discretionary", FlowsToward = "Consumer Staples", Mechanism = "Late-cycle rotation into defensives" }
            ]
        };

        var result = await _sut.AnalyzeAsync(substitutionChain);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Targets, Is.Not.Empty);
        foreach (var t in result.Targets)
        {
            Assert.That(t.Category, Is.Not.Empty);
            Assert.That(t.Rationale, Is.Not.Empty);
        }

        Assert.That(result.InputTokens, Is.GreaterThan(0), "input tokens should be captured from the LLM response");
        Assert.That(result.OutputTokens, Is.GreaterThan(0), "output tokens should be captured from the LLM response");
        Assert.That(result.TotalTokens, Is.GreaterThanOrEqualTo(result.InputTokens + result.OutputTokens));
    }
}
