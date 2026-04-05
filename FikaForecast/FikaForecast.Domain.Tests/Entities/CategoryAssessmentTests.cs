using FikaForecast.Domain.Entities;
using FikaForecast.Domain.Enums;

namespace FikaForecast.Domain.Tests.Entities;

[TestFixture]
[TestOf(typeof(CategoryAssessment))]
public class CategoryAssessmentTests
{
    [Test]
    public void Constructor_SetsAllProperties()
    {
        // Act
        var assessment = new CategoryAssessment(
            "Geopolitics / Energy",
            "US-Israel war on Iran",
            "Energy supply concerns dominate trading.",
            MarketSentiment.RiskOff);

        // Assert
        Assert.That(assessment.AssessmentId, Is.Not.EqualTo(Guid.Empty));
        Assert.That(assessment.Category, Is.EqualTo("Geopolitics / Energy"));
        Assert.That(assessment.Headline, Is.EqualTo("US-Israel war on Iran"));
        Assert.That(assessment.Summary, Is.EqualTo("Energy supply concerns dominate trading."));
        Assert.That(assessment.Sentiment, Is.EqualTo(MarketSentiment.RiskOff));
    }

    [TestCase(MarketSentiment.RiskOff)]
    [TestCase(MarketSentiment.RiskOn)]
    [TestCase(MarketSentiment.Mixed)]
    public void Constructor_AllSentimentValues_Accepted(MarketSentiment sentiment)
    {
        // Act
        var assessment = new CategoryAssessment("Test", "Headline", "Summary", sentiment);

        // Assert
        Assert.That(assessment.Sentiment, Is.EqualTo(sentiment));
    }

    [Test]
    public void Constructor_FreeTextCategory_PreservedAsIs()
    {
        // Act
        var assessment = new CategoryAssessment(
            "Some Unexpected Category / Subcategory",
            "Headline",
            "Summary",
            MarketSentiment.Mixed);

        // Assert
        Assert.That(assessment.Category, Is.EqualTo("Some Unexpected Category / Subcategory"));
    }
}
