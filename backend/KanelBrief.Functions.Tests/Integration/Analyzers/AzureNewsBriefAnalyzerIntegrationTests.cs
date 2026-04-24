using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.Identity;
using KanelBrief.Functions.Agents.Analyzers;
using KanelBrief.Functions.Orchestration;
using Microsoft.Extensions.Logging.Abstractions;

namespace KanelBrief.Functions.Tests.Integration.Analyzers;

/// <summary>
/// Hits live Azure AI Foundry. Prerequisites:
/// <list type="bullet">
///   <item><description><c>FOUNDRY_PROJECT_ENDPOINT</c> in user secrets</description></item>
///   <item><description><c>az login</c> for <c>DefaultAzureCredential</c></description></item>
///   <item><description>Persistent agent <c>kanelbrief-news-brief</c> created in the Foundry portal
///     (see <c>docs/AZURE-DEPLOYMENT.md § Persistent Agent Setup</c>) — the test fails fast with a
///     clear error message if it's missing, but we'd rather skip the wasted call.</description></item>
/// </list>
/// Run manually with: <c>dotnet test --filter "TestCategory=Integration"</c>.
/// </summary>
[TestFixture]
[Explicit("Hits live Azure AI Foundry — run manually. Requires user secrets and the " +
          "persistent 'kanelbrief-news-brief' agent in the portal (see docs/AZURE-DEPLOYMENT.md).")]
[Category("Integration")]
public class AzureNewsBriefAnalyzerIntegrationTests
{
    private AzureNewsBriefAnalyzer _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var config = IntegrationConfig.Load();
        var endpoint = IntegrationConfig.RequireFoundryEndpoint(config);
        var credential = new DefaultAzureCredential();

        var aiProjectClient = new AIProjectClient(endpoint, credential);
        var agentAdmin = new AgentAdministrationClient(endpoint, credential);
        var options = new OrchestratorOptions
        {
            FoundryEndpoint = endpoint,
            Credential = credential,
            BingConnectionName = IntegrationConfig.GetBingConnectionName(config)
        };

        _sut = new AzureNewsBriefAnalyzer(
            NullLogger<AzureNewsBriefAnalyzer>.Instance,
            aiProjectClient,
            agentAdmin,
            options);
    }

    [Test]
    public async Task AnalyzeAsync_AgainstLiveFoundry_ReturnsParsableResult()
    {
        var result = await _sut.AnalyzeAsync(DateTimeOffset.UtcNow);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Summary, Is.Not.Empty, "summary should not be empty");
        Assert.That(result.Mood, Is.AnyOf("RiskOn", "RiskOff", "Mixed"));
        Assert.That(result.Assessments, Is.Not.Empty, "should produce at least one sector assessment");
        foreach (var a in result.Assessments)
        {
            Assert.That(a.Category, Is.Not.Empty);
        }

        Assert.That(result.InputTokens, Is.GreaterThan(0), "input tokens should be captured from the LLM response");
        Assert.That(result.OutputTokens, Is.GreaterThan(0), "output tokens should be captured from the LLM response");
        Assert.That(result.TotalTokens, Is.GreaterThanOrEqualTo(result.InputTokens + result.OutputTokens));
    }
}
