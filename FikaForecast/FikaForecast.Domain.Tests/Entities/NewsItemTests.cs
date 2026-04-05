using FikaForecast.Domain.Entities;
using FikaForecast.Domain.Enums;

namespace FikaForecast.Domain.Tests.Entities;

[TestFixture]
[TestOf(typeof(NewsItem))]
public class NewsItemTests
{
    [Test]
    public void Constructor_SetsMoodAndSummary()
    {
        // Act
        var item = new NewsItem(MarketSentiment.RiskOn, "Markets bullish on AI");

        // Assert
        Assert.That(item.ItemId, Is.Not.EqualTo(Guid.Empty));
        Assert.That(item.Mood, Is.EqualTo(MarketSentiment.RiskOn));
        Assert.That(item.Summary, Is.EqualTo("Markets bullish on AI"));
        Assert.That(item.Assessments, Is.Empty);
    }

    [Test]
    public void AddAssessment_AddsToCollection()
    {
        // Arrange
        var item = new NewsItem(MarketSentiment.Mixed, "Mixed signals");
        var assessment = new CategoryAssessment("Tech / AI", "Nvidia GTC", "AI capex rising", MarketSentiment.RiskOn);

        // Act
        item.AddAssessment(assessment);

        // Assert
        Assert.That(item.Assessments, Has.Count.EqualTo(1));
        Assert.That(item.Assessments[0], Is.SameAs(assessment));
    }

    [Test]
    public void AddAssessment_MultipleAssessments_PreservesOrder()
    {
        // Arrange
        var item = new NewsItem(MarketSentiment.RiskOff, "Risk-off");
        var a1 = new CategoryAssessment("Geopolitics", "War", "Impact", MarketSentiment.RiskOff);
        var a2 = new CategoryAssessment("Central Banks", "Fed", "Rates", MarketSentiment.RiskOff);

        // Act
        item.AddAssessment(a1);
        item.AddAssessment(a2);

        // Assert
        Assert.That(item.Assessments, Has.Count.EqualTo(2));
        Assert.That(item.Assessments[0].Category, Is.EqualTo("Geopolitics"));
        Assert.That(item.Assessments[1].Category, Is.EqualTo("Central Banks"));
    }
}
