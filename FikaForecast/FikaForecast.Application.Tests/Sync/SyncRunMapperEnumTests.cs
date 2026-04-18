using FikaForecast.Application.Sync.Mappers;
using FikaForecast.Domain.Enums;

namespace FikaForecast.Application.Tests.Sync;

/// <summary>
/// Unit tests for the string-based enum translations in <see cref="SyncRunMapper"/>.
/// Backend emits enum names like <c>"Success"</c>, <c>"Medium"</c>, <c>"Weak"</c>;
/// some of those don't exist WPF-side and must fold to the closest equivalent.
/// </summary>
[TestFixture]
[TestOf(typeof(SyncRunMapper))]
public class SyncRunMapperEnumTests
{
    [TestCase("Success", RunStatus.Success)]
    [TestCase("Failed", RunStatus.Failed)]
    [TestCase("Partial", RunStatus.Failed)]       // obsolete backend value → Failed
    [TestCase("Unknown", RunStatus.Failed)]
    [TestCase("", RunStatus.Failed)]
    public void MapStatus_MapsBackendNameToWpfEnum(string backendName, RunStatus expected)
    {
        // Arrange
        // (backendName / expected provided by TestCase)

        // Act
        var result = SyncRunMapper.MapStatus(backendName);

        // Assert
        Assert.That(result, Is.EqualTo(expected));
    }

    [TestCase("RiskOn", MarketSentiment.RiskOn)]
    [TestCase("RiskOff", MarketSentiment.RiskOff)]
    [TestCase("Mixed", MarketSentiment.Mixed)]
    [TestCase("Garbage", MarketSentiment.Mixed)]
    public void MapSentiment_MapsBackendNameToWpfEnum(string backendName, MarketSentiment expected)
    {
        // Arrange
        // (backendName / expected provided by TestCase)

        // Act
        var result = SyncRunMapper.MapSentiment(backendName);

        // Assert
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void MapMoodString_UsesSameMappingAsSentiment()
    {
        // Arrange
        const string riskOnName = "RiskOn";
        const string riskOffName = "RiskOff";

        // Act
        var riskOn = SyncRunMapper.MapMoodString(riskOnName);
        var riskOff = SyncRunMapper.MapMoodString(riskOffName);

        // Assert
        Assert.That(riskOn, Is.EqualTo(MarketSentiment.RiskOn));
        Assert.That(riskOff, Is.EqualTo(MarketSentiment.RiskOff));
    }

    [TestCase("High", ConfidenceLevel.High)]
    [TestCase("Medium", ConfidenceLevel.Moderate)]
    [TestCase("Low", ConfidenceLevel.Dropped)]
    [TestCase("Unknown", ConfidenceLevel.Moderate)]
    public void MapConfidence_MapsBackendNameToWpfEnum(string backendName, ConfidenceLevel expected)
    {
        // Arrange
        // (backendName / expected provided by TestCase)

        // Act
        var result = SyncRunMapper.MapConfidence(backendName);

        // Assert
        Assert.That(result, Is.EqualTo(expected));
    }

    [TestCase("Strong", SignalStrength.Strong)]
    [TestCase("Moderate", SignalStrength.Moderate)]
    [TestCase("Weak", SignalStrength.Moderate)]     // no WPF equivalent for Weak
    [TestCase("Unknown", SignalStrength.Moderate)]
    public void MapSignalStrength_MapsBackendNameToWpfEnum(string backendName, SignalStrength expected)
    {
        // Arrange
        // (backendName / expected provided by TestCase)

        // Act
        var result = SyncRunMapper.MapSignalStrength(backendName);

        // Assert
        Assert.That(result, Is.EqualTo(expected));
    }
}
