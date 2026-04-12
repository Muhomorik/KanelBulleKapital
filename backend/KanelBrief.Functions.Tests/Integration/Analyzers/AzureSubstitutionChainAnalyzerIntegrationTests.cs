using Azure.AI.Projects;
using Azure.Identity;
using KanelBrief.Core.Models;
using KanelBrief.Functions.Agents.Analyzers;
using Microsoft.Extensions.Logging.Abstractions;

namespace KanelBrief.Functions.Tests.Integration.Analyzers;

/// <summary>
/// Hits live Azure AI Foundry. Requires FOUNDRY_PROJECT_ENDPOINT in user secrets and <c>az login</c>.
/// Run manually with: <c>dotnet test --filter "TestCategory=Integration"</c>.
/// </summary>
[TestFixture]
[Explicit("Hits live Azure AI Foundry — run manually, requires user secrets.")]
[Category("Integration")]
public class AzureSubstitutionChainAnalyzerIntegrationTests
{
    private AzureSubstitutionChainAnalyzer _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var config = IntegrationConfig.Load();
        var endpoint = IntegrationConfig.RequireFoundryEndpoint(config);
        var aiProjectClient = new AIProjectClient(endpoint, new DefaultAzureCredential());

        _sut = new AzureSubstitutionChainAnalyzer(
            NullLogger<AzureSubstitutionChainAnalyzer>.Instance,
            aiProjectClient);
    }

    [Test]
    public async Task AnalyzeAsync_AgainstLiveFoundry_ReturnsParsableResult()
    {
        var weeklySummary = new WeeklySummaryRun
        {
            RunId = Guid.NewGuid().ToString(),
            RunDate = "2026-04-06",
            NetMood = MarketSentiment.Mixed,
            MoodSummary = "Rotation from growth into defensives mid-week as yields spiked.",
            Themes =
            [
                new WeeklySummaryTheme { Category = "Rising yields", Summary = "10Y broke 4.5%", Confidence = ConfidenceLevel.High, Sentiment = MarketSentiment.RiskOff },
                new WeeklySummaryTheme { Category = "AI capex", Summary = "Hyperscalers still buying", Confidence = ConfidenceLevel.Medium, Sentiment = MarketSentiment.RiskOn }
            ]
        };

        var result = await _sut.AnalyzeAsync(weeklySummary);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Chains, Is.Not.Empty);
        foreach (var chain in result.Chains)
        {
            Assert.That(chain.CapitalFleeing, Is.Not.Empty);
            Assert.That(chain.FlowsToward, Is.Not.Empty);
        }
    }
}
