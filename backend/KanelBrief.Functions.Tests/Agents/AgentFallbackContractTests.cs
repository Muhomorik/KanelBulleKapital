using KanelBrief.Core.Models;
using KanelBrief.Functions.Agents;

namespace KanelBrief.Functions.Tests.Agents;

/// <summary>
/// Verifies the hardcoded fallback data contracts for agents.
/// These are the safety nets when LLM calls fail — their shape matters for frontend rendering.
/// </summary>
[TestFixture]
public class AgentFallbackContractTests
{
    [Test]
    public void WeeklySummaryAgent_GenerateFallbackThemes_ReturnsExactlyOneTheme()
    {
        var themes = WeeklySummaryAgent.GenerateFallbackThemes();
        Assert.That(themes, Has.Count.EqualTo(1));
    }

    [Test]
    public void WeeklySummaryAgent_GenerateFallbackThemes_ThemeIsTechnology_HighConfidence_RiskOn()
    {
        var theme = WeeklySummaryAgent.GenerateFallbackThemes()[0];

        Assert.That(theme.Category, Is.EqualTo("Technology"));
        Assert.That(theme.Confidence, Is.EqualTo(ConfidenceLevel.High));
        Assert.That(theme.Sentiment, Is.EqualTo(MarketSentiment.RiskOn));
    }

    [Test]
    public void SubstitutionChainAgent_GenerateFallbackChains_ReturnsExactlyOneChain()
    {
        var chains = SubstitutionChainAgent.GenerateFallbackChains();
        Assert.That(chains, Has.Count.EqualTo(1));
    }

    [Test]
    public void SubstitutionChainAgent_GenerateFallbackChains_EnergyFleeingToTechnology()
    {
        var chain = SubstitutionChainAgent.GenerateFallbackChains()[0];

        Assert.That(chain.CapitalFleeing, Is.EqualTo("Energy"));
        Assert.That(chain.FlowsToward, Is.EqualTo("Technology"));
    }

    [Test]
    public void OpportunityScanAgent_GenerateFallbackTargets_ReturnsExactlyOneTarget()
    {
        var targets = OpportunityScanAgent.GenerateFallbackTargets();
        Assert.That(targets, Has.Count.EqualTo(1));
    }

    [Test]
    public void OpportunityScanAgent_GenerateFallbackTargets_CloudInfrastructure_StrongSignal()
    {
        var target = OpportunityScanAgent.GenerateFallbackTargets()[0];

        Assert.That(target.Category, Does.Contain("Cloud Infrastructure"));
        Assert.That(target.SignalStrength, Is.EqualTo(SignalStrength.Strong));
    }
}
