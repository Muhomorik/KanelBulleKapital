using System.Text.Json;
using KanelBrief.Core.Models;

namespace KanelBrief.Core.Tests.Models;

[TestFixture]
public class EnumSerializationTests
{
    [TestCase(RunStatus.Success, "\"Success\"")]
    [TestCase(RunStatus.Failed, "\"Failed\"")]
    [TestCase(RunStatus.Partial, "\"Partial\"")]
    public void RunStatus_Serialize_ProducesStringNotInteger(RunStatus value, string expected)
    {
        var json = JsonSerializer.Serialize(value);
        Assert.That(json, Is.EqualTo(expected));
    }

    [TestCase("\"Success\"", RunStatus.Success)]
    [TestCase("\"Failed\"", RunStatus.Failed)]
    [TestCase("\"Partial\"", RunStatus.Partial)]
    public void RunStatus_DeserializeFromString_ReturnsCorrectValue(string json, RunStatus expected)
    {
        var result = JsonSerializer.Deserialize<RunStatus>(json);
        Assert.That(result, Is.EqualTo(expected));
    }

    [TestCase(MarketSentiment.RiskOn, "\"RiskOn\"")]
    [TestCase(MarketSentiment.RiskOff, "\"RiskOff\"")]
    [TestCase(MarketSentiment.Mixed, "\"Mixed\"")]
    public void MarketSentiment_RoundTrip_PreservedThroughJson(MarketSentiment value, string expectedJson)
    {
        var json = JsonSerializer.Serialize(value);
        Assert.That(json, Is.EqualTo(expectedJson));

        var deserialized = JsonSerializer.Deserialize<MarketSentiment>(json);
        Assert.That(deserialized, Is.EqualTo(value));
    }

    [TestCase(ConfidenceLevel.High)]
    [TestCase(ConfidenceLevel.Medium)]
    [TestCase(ConfidenceLevel.Low)]
    public void ConfidenceLevel_RoundTrip_AllValues_PreservedThroughJson(ConfidenceLevel value)
    {
        var json = JsonSerializer.Serialize(value);
        var deserialized = JsonSerializer.Deserialize<ConfidenceLevel>(json);
        Assert.That(deserialized, Is.EqualTo(value));
    }

    [TestCase(SignalStrength.Strong)]
    [TestCase(SignalStrength.Moderate)]
    [TestCase(SignalStrength.Weak)]
    public void SignalStrength_RoundTrip_AllValues_PreservedThroughJson(SignalStrength value)
    {
        var json = JsonSerializer.Serialize(value);
        var deserialized = JsonSerializer.Deserialize<SignalStrength>(json);
        Assert.That(deserialized, Is.EqualTo(value));
    }
}
