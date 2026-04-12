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
public class AzureWeeklySummaryAnalyzerIntegrationTests
{
    private AzureWeeklySummaryAnalyzer _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var config = IntegrationConfig.Load();
        var endpoint = IntegrationConfig.RequireFoundryEndpoint(config);
        var aiProjectClient = new AIProjectClient(endpoint, new DefaultAzureCredential());

        _sut = new AzureWeeklySummaryAnalyzer(
            NullLogger<AzureWeeklySummaryAnalyzer>.Instance,
            aiProjectClient);
    }

    [Test]
    public async Task AnalyzeAsync_AgainstLiveFoundry_ReturnsParsableResult()
    {
        var weekStart = new DateTime(2026, 3, 30);
        var weekEnd = new DateTime(2026, 4, 6);
        var briefs = new List<NewsBriefRun>
        {
            new()
            {
                RunDate = "2026-03-30",
                Mood = "RiskOn",
                Summary = "Equities rally on strong earnings.",
                Assessments =
                [
                    new CategoryAssessment { Category = "Technology", Headline = "AI momentum", Summary = "Mega-caps lead", Sentiment = MarketSentiment.RiskOn }
                ]
            },
            new()
            {
                RunDate = "2026-04-01",
                Mood = "Mixed",
                Summary = "Rotation out of tech into energy.",
                Assessments =
                [
                    new CategoryAssessment { Category = "Energy", Headline = "Oil rebound", Summary = "Supply cuts", Sentiment = MarketSentiment.RiskOn }
                ]
            }
        };

        var result = await _sut.AnalyzeAsync(weekStart, weekEnd, briefs);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Summary, Is.Not.Empty);
        Assert.That(result.Mood, Is.AnyOf("RiskOn", "RiskOff", "Mixed"));
        Assert.That(result.Themes, Is.Not.Empty);
    }
}
