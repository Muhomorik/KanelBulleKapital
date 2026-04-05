using AutoFixture;
using AutoFixture.AutoMoq;
using FikaForecast.Application.Services;
using FikaForecast.Domain.Enums;
using Moq;
using NLog;

namespace FikaForecast.Application.Tests.Services;

[TestFixture]
[TestOf(typeof(NewsBriefJsonParser))]
public class NewsBriefJsonParserTests
{
    private IFixture _fixture = null!;
    private NewsBriefJsonParser _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture().Customize(new AutoMoqCustomization());
        _fixture.Freeze<Mock<ILogger>>();
        _sut = _fixture.Create<NewsBriefJsonParser>();
    }

    private const string ValidJson = """
        {
            "mood": "RiskOff",
            "summary": "Risk-off, energy and inflation dominate.",
            "categories": [
                {
                    "category": "Geopolitics / Energy",
                    "headline": "US-Israel war on Iran",
                    "summary": "Energy supply concerns.",
                    "sentiment": "RiskOff"
                },
                {
                    "category": "Tech / AI",
                    "headline": "Nvidia GTC 2026",
                    "summary": "AI capex rising.",
                    "sentiment": "RiskOn"
                }
            ]
        }
        """;

    #region Happy Path

    [Test]
    public void Parse_ValidJson_ReturnsItemWithMoodAndAssessments()
    {
        // Act
        var result = _sut.Parse(ValidJson);

        // Assert
        Assert.That(result.Item, Is.Not.Null);
        Assert.That(result.Item!.Mood, Is.EqualTo(MarketSentiment.RiskOff));
        Assert.That(result.Item.Summary, Is.EqualTo("Risk-off, energy and inflation dominate."));
        Assert.That(result.Item.Assessments, Has.Count.EqualTo(2));
        Assert.That(result.IsComplete, Is.True);
        Assert.That(result.Warnings, Is.Empty);
    }

    [Test]
    public void Parse_ValidJson_AssessmentsHaveCorrectData()
    {
        // Act
        var result = _sut.Parse(ValidJson);
        var assessments = result.Item!.Assessments;

        // Assert
        Assert.That(assessments[0].Category, Is.EqualTo("Geopolitics / Energy"));
        Assert.That(assessments[0].Headline, Is.EqualTo("US-Israel war on Iran"));
        Assert.That(assessments[0].Sentiment, Is.EqualTo(MarketSentiment.RiskOff));

        Assert.That(assessments[1].Category, Is.EqualTo("Tech / AI"));
        Assert.That(assessments[1].Sentiment, Is.EqualTo(MarketSentiment.RiskOn));
    }

    #endregion

    #region Sentiment Mapping

    [TestCase("RiskOff", MarketSentiment.RiskOff)]
    [TestCase("RiskOn", MarketSentiment.RiskOn)]
    [TestCase("Mixed", MarketSentiment.Mixed)]
    [TestCase("riskoff", MarketSentiment.RiskOff)]
    [TestCase("RISKON", MarketSentiment.RiskOn)]
    public void Parse_SentimentValues_MappedCaseInsensitive(string sentimentText, MarketSentiment expected)
    {
        // Arrange
        var json = $$"""
            {
                "mood": "{{sentimentText}}",
                "summary": "Test",
                "categories": [
                    { "category": "Test", "headline": "H", "summary": "S", "sentiment": "{{sentimentText}}" }
                ]
            }
            """;

        // Act
        var result = _sut.Parse(json);

        // Assert
        Assert.That(result.Item!.Mood, Is.EqualTo(expected));
        Assert.That(result.Item.Assessments[0].Sentiment, Is.EqualTo(expected));
    }

    [Test]
    public void Parse_UnknownSentiment_DefaultsToMixedWithWarning()
    {
        // Arrange
        var json = """
            {
                "mood": "Bearish",
                "summary": "Test",
                "categories": [
                    { "category": "Test", "headline": "H", "summary": "S", "sentiment": "Bullish" }
                ]
            }
            """;

        // Act
        var result = _sut.Parse(json);

        // Assert
        Assert.That(result.Item!.Mood, Is.EqualTo(MarketSentiment.Mixed));
        Assert.That(result.Item.Assessments[0].Sentiment, Is.EqualTo(MarketSentiment.Mixed));
        Assert.That(result.IsComplete, Is.False);
        Assert.That(result.Warnings, Has.Count.GreaterThanOrEqualTo(2));
    }

    #endregion

    #region Category as Free Text

    [Test]
    public void Parse_FreeTextCategory_PreservedAsIs()
    {
        // Arrange
        var json = """
            {
                "mood": "Mixed",
                "summary": "Test",
                "categories": [
                    { "category": "Some New Category / Subcategory", "headline": "H", "summary": "S", "sentiment": "Mixed" }
                ]
            }
            """;

        // Act
        var result = _sut.Parse(json);

        // Assert
        Assert.That(result.Item!.Assessments[0].Category, Is.EqualTo("Some New Category / Subcategory"));
    }

    #endregion

    #region Resilience

    [Test]
    public void Parse_InvalidJson_ReturnsNullItemWithWarning()
    {
        // Act
        var result = _sut.Parse("not valid json {{{");

        // Assert
        Assert.That(result.Item, Is.Null);
        Assert.That(result.IsComplete, Is.False);
        Assert.That(result.Warnings, Has.Count.EqualTo(1));
    }

    [Test]
    public void Parse_EmptyString_ReturnsNullItemWithWarning()
    {
        // Act
        var result = _sut.Parse("");

        // Assert
        Assert.That(result.Item, Is.Null);
        Assert.That(result.IsComplete, Is.False);
    }

    [Test]
    public void Parse_NullString_ReturnsNullItemWithWarning()
    {
        // Act
        var result = _sut.Parse(null!);

        // Assert
        Assert.That(result.Item, Is.Null);
        Assert.That(result.IsComplete, Is.False);
    }

    [Test]
    public void Parse_EmptyCategories_ReturnsItemWithWarning()
    {
        // Arrange
        var json = """{ "mood": "Mixed", "summary": "Test", "categories": [] }""";

        // Act
        var result = _sut.Parse(json);

        // Assert
        Assert.That(result.Item, Is.Not.Null);
        Assert.That(result.Item!.Assessments, Is.Empty);
        Assert.That(result.IsComplete, Is.False);
        Assert.That(result.Warnings, Does.Contain("No categories found in JSON output"));
    }

    [Test]
    public void Parse_EmptyHeadline_SkipsWithWarning()
    {
        // Arrange
        var json = """
            {
                "mood": "Mixed",
                "summary": "Test",
                "categories": [
                    { "category": "Test", "headline": "", "summary": "S", "sentiment": "Mixed" },
                    { "category": "Valid", "headline": "Valid headline", "summary": "S", "sentiment": "RiskOn" }
                ]
            }
            """;

        // Act
        var result = _sut.Parse(json);

        // Assert
        Assert.That(result.Item!.Assessments, Has.Count.EqualTo(1));
        Assert.That(result.Item.Assessments[0].Headline, Is.EqualTo("Valid headline"));
    }

    #endregion
}
