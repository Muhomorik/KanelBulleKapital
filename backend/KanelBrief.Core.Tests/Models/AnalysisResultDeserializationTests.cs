using System.Text.Json;
using KanelBrief.Core.Models;

namespace KanelBrief.Core.Tests.Models;

[TestFixture]
public class AnalysisResultDeserializationTests
{
    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Test]
    public void NewsBriefAnalysisResult_DeserializeCamelCase_MapsAllFields()
    {
        var json = """
        {
          "mood": "RiskOn",
          "summary": "Markets rally on tech earnings",
          "assessments": [
            {
              "category": "Technology",
              "headline": "AI spending surges",
              "summary": "Cloud capex beats expectations",
              "sentiment": "RiskOn"
            }
          ]
        }
        """;

        var result = JsonSerializer.Deserialize<NewsBriefAnalysisResult>(json, _options)!;

        Assert.That(result.Mood, Is.EqualTo("RiskOn"));
        Assert.That(result.Summary, Is.EqualTo("Markets rally on tech earnings"));
        Assert.That(result.Assessments, Has.Count.EqualTo(1));
        Assert.That(result.Assessments[0].Category, Is.EqualTo("Technology"));
        Assert.That(result.Assessments[0].Sentiment, Is.EqualTo(MarketSentiment.RiskOn));
    }

    [Test]
    public void WeeklySummaryAnalysisResult_DeserializeCamelCase_ThemesWithNestedEnums()
    {
        var json = """
        {
          "mood": "Mixed",
          "summary": "Volatile week with mixed signals",
          "themes": [
            {
              "category": "Trade War",
              "summary": "Tariff escalation fears",
              "confidence": "High",
              "sentiment": "RiskOff"
            }
          ]
        }
        """;

        var result = JsonSerializer.Deserialize<WeeklySummaryAnalysisResult>(json, _options)!;

        Assert.That(result.Mood, Is.EqualTo("Mixed"));
        Assert.That(result.Themes, Has.Count.EqualTo(1));
        Assert.That(result.Themes[0].Confidence, Is.EqualTo(ConfidenceLevel.High));
        Assert.That(result.Themes[0].Sentiment, Is.EqualTo(MarketSentiment.RiskOff));
    }

    [Test]
    public void SubstitutionChainAnalysisResult_DeserializeCamelCase_ChainsPreserved()
    {
        var json = """
        {
          "chains": [
            {
              "capitalFleeing": "Energy",
              "flowsToward": "Technology",
              "mechanism": "ESG-driven reallocation"
            }
          ]
        }
        """;

        var result = JsonSerializer.Deserialize<SubstitutionChainAnalysisResult>(json, _options)!;

        Assert.That(result.Chains, Has.Count.EqualTo(1));
        Assert.That(result.Chains[0].CapitalFleeing, Is.EqualTo("Energy"));
        Assert.That(result.Chains[0].FlowsToward, Is.EqualTo("Technology"));
        Assert.That(result.Chains[0].Mechanism, Is.EqualTo("ESG-driven reallocation"));
    }

    [Test]
    public void OpportunityScanAnalysisResult_DeserializeCamelCase_TargetsWithSignalStrength()
    {
        var json = """
        {
          "targets": [
            {
              "category": "Cloud Infrastructure",
              "signalStrength": "Strong",
              "rationale": "Sustained demand",
              "riskCaveat": "High valuations"
            }
          ]
        }
        """;

        var result = JsonSerializer.Deserialize<OpportunityScanAnalysisResult>(json, _options)!;

        Assert.That(result.Targets, Has.Count.EqualTo(1));
        Assert.That(result.Targets[0].SignalStrength, Is.EqualTo(SignalStrength.Strong));
        Assert.That(result.Targets[0].RiskCaveat, Is.EqualTo("High valuations"));
    }

    [Test]
    public void NewsBriefAnalysisResult_EmptyAssessments_DeserializesToEmptyList()
    {
        var json = """{"mood":"Mixed","summary":"Quiet day","assessments":[]}""";

        var result = JsonSerializer.Deserialize<NewsBriefAnalysisResult>(json, _options)!;

        Assert.That(result.Assessments, Is.Not.Null);
        Assert.That(result.Assessments, Is.Empty);
    }
}
