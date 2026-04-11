using KanelBrief.Core.Models;
using KanelBrief.Core.Parsers;

namespace KanelBrief.Core.Tests.Parsers;

[TestFixture]
[TestOf(typeof(AgentResponseParser))]
public class AgentResponseParserTests
{
    // ── ExtractJson ──

    [Test]
    public void ExtractJson_PureJson_ReturnsSameString()
    {
        var json = """{"mood":"RiskOn"}""";
        Assert.That(AgentResponseParser.ExtractJson(json), Is.EqualTo(json));
    }

    [Test]
    public void ExtractJson_JsonWrappedInText_ExtractsJsonPortion()
    {
        var input = """Here is the analysis:\n{"mood":"RiskOn","summary":"bull run"}\nDone.""";
        var result = AgentResponseParser.ExtractJson(input);
        Assert.That(result, Does.StartWith("{"));
        Assert.That(result, Does.EndWith("}"));
        Assert.That(result, Does.Contain("RiskOn"));
    }

    [Test]
    public void ExtractJson_NestedBraces_ReturnsOutermostObject()
    {
        var input = """{"assessments":[{"category":"Tech","sentiment":"RiskOn"}]}""";
        var result = AgentResponseParser.ExtractJson(input);
        Assert.That(result, Is.EqualTo(input));
    }

    [Test]
    public void ExtractJson_NoBraces_ThrowsInvalidOperationException()
    {
        Assert.That(
            () => AgentResponseParser.ExtractJson("No JSON here at all"),
            Throws.TypeOf<InvalidOperationException>()
                .With.Message.Contains("No JSON found"));
    }

    [Test]
    public void ExtractJson_OnlyOpenBrace_ThrowsInvalidOperationException()
    {
        Assert.That(
            () => AgentResponseParser.ExtractJson("prefix { no closing brace"),
            Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void ExtractJson_MarkdownCodeBlock_ExtractsJsonInsideBlock()
    {
        var input = "```json\n{\"mood\":\"Mixed\"}\n```";
        var result = AgentResponseParser.ExtractJson(input);
        Assert.That(result, Is.EqualTo("{\"mood\":\"Mixed\"}"));
    }

    [Test]
    public void ExtractJson_EmptyString_ThrowsInvalidOperationException()
    {
        Assert.That(
            () => AgentResponseParser.ExtractJson(string.Empty),
            Throws.TypeOf<InvalidOperationException>());
    }

    // ── ParseSentiment ──

    [TestCase("riskon", MarketSentiment.RiskOn)]
    [TestCase("RiskOn", MarketSentiment.RiskOn)]
    [TestCase("RISKON", MarketSentiment.RiskOn)]
    [TestCase("riskoff", MarketSentiment.RiskOff)]
    [TestCase("RiskOff", MarketSentiment.RiskOff)]
    [TestCase("mixed", MarketSentiment.Mixed)]
    [TestCase("Mixed", MarketSentiment.Mixed)]
    public void ParseSentiment_KnownValues_ReturnsCorrectEnum(string input, MarketSentiment expected)
    {
        Assert.That(AgentResponseParser.ParseSentiment(input), Is.EqualTo(expected));
    }

    [TestCase("bullish")]
    [TestCase("neutral")]
    [TestCase("")]
    [TestCase("unknown")]
    public void ParseSentiment_UnrecognizedValue_DefaultsToMixed(string input)
    {
        Assert.That(AgentResponseParser.ParseSentiment(input), Is.EqualTo(MarketSentiment.Mixed));
    }

    // ── ParseConfidence ──

    [TestCase("high", ConfidenceLevel.High)]
    [TestCase("High", ConfidenceLevel.High)]
    [TestCase("medium", ConfidenceLevel.Medium)]
    [TestCase("low", ConfidenceLevel.Low)]
    public void ParseConfidence_KnownValues_ReturnsCorrectEnum(string input, ConfidenceLevel expected)
    {
        Assert.That(AgentResponseParser.ParseConfidence(input), Is.EqualTo(expected));
    }

    [TestCase("uncertain")]
    [TestCase("")]
    [TestCase("UNKNOWN")]
    public void ParseConfidence_UnrecognizedValue_DefaultsToMedium(string input)
    {
        Assert.That(AgentResponseParser.ParseConfidence(input), Is.EqualTo(ConfidenceLevel.Medium));
    }

    // ── ParseSignalStrength ──

    [TestCase("strong", SignalStrength.Strong)]
    [TestCase("Strong", SignalStrength.Strong)]
    [TestCase("STRONG", SignalStrength.Strong)]
    [TestCase("moderate", SignalStrength.Moderate)]
    [TestCase("weak", SignalStrength.Weak)]
    public void ParseSignalStrength_KnownValues_ReturnsCorrectEnum(string input, SignalStrength expected)
    {
        Assert.That(AgentResponseParser.ParseSignalStrength(input), Is.EqualTo(expected));
    }

    [TestCase("medium")]
    [TestCase("")]
    [TestCase("average")]
    public void ParseSignalStrength_UnrecognizedValue_DefaultsToModerate(string input)
    {
        Assert.That(AgentResponseParser.ParseSignalStrength(input), Is.EqualTo(SignalStrength.Moderate));
    }
}
