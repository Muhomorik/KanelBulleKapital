using FikaForecast.Application.DTOs;
using FikaForecast.Application.Services;
using FikaForecast.Domain.Entities;
using FikaForecast.Domain.Enums;

namespace FikaForecast.Application.Tests.Services;

[TestFixture]
[TestOf(typeof(NewsBriefMarkdownRenderer))]
public class NewsBriefMarkdownRendererTests
{
    private NewsBriefMarkdownRenderer _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _sut = new NewsBriefMarkdownRenderer();
    }

    #region Rendering

    [Test]
    public void Render_GroupsAssessmentsByCategoryWithEmojis()
    {
        // Arrange
        var item = new NewsItem(MarketSentiment.RiskOff, "Risk-off mood");
        item.AddAssessment(new CategoryAssessment("Geopolitics / Energy", "War", "Impact", MarketSentiment.RiskOff));
        item.AddAssessment(new CategoryAssessment("Geopolitics / Energy", "Oil", "Prices up", MarketSentiment.RiskOff));
        item.AddAssessment(new CategoryAssessment("Tech / AI", "Nvidia", "AI capex", MarketSentiment.RiskOn));
        var parseResult = new NewsBriefParseResult(item, true, []);

        // Act
        var markdown = _sut.Render(parseResult);

        // Assert
        Assert.That(markdown, Does.Contain("🔴 GEOPOLITICS / ENERGY"));
        Assert.That(markdown, Does.Contain("- **War** — Impact"));
        Assert.That(markdown, Does.Contain("- **Oil** — Prices up"));
        Assert.That(markdown, Does.Contain("🟢 TECH / AI"));
        Assert.That(markdown, Does.Contain("- **Nvidia** — AI capex"));
    }

    [Test]
    public void Render_OverallMoodLine_IncludesEmojiAndSummary()
    {
        // Arrange
        var item = new NewsItem(MarketSentiment.RiskOff, "Risk-off, energy dominates.");
        item.AddAssessment(new CategoryAssessment("Test", "H", "S", MarketSentiment.RiskOff));
        var parseResult = new NewsBriefParseResult(item, true, []);

        // Act
        var markdown = _sut.Render(parseResult);

        // Assert
        Assert.That(markdown, Does.Contain("OVERALL MOOD: 🔴 Risk-off, energy dominates."));
    }

    [TestCase(MarketSentiment.RiskOff, "🔴")]
    [TestCase(MarketSentiment.RiskOn, "🟢")]
    [TestCase(MarketSentiment.Mixed, "🟡")]
    public void Render_MoodEmoji_MatchesSentiment(MarketSentiment mood, string expectedEmoji)
    {
        // Arrange
        var item = new NewsItem(mood, "Summary");
        item.AddAssessment(new CategoryAssessment("Test", "H", "S", mood));
        var parseResult = new NewsBriefParseResult(item, true, []);

        // Act
        var markdown = _sut.Render(parseResult);

        // Assert
        Assert.That(markdown, Does.Contain($"OVERALL MOOD: {expectedEmoji}"));
    }

    #endregion

    #region Edge Cases

    [Test]
    public void Render_NullItem_ReturnsEmpty()
    {
        // Arrange
        var parseResult = new NewsBriefParseResult(null, false, ["Error"]);

        // Act
        var markdown = _sut.Render(parseResult);

        // Assert
        Assert.That(markdown, Is.Empty);
    }

    [Test]
    public void Render_EmptyAssessments_ReturnsEmpty()
    {
        // Arrange
        var item = new NewsItem(MarketSentiment.Mixed, "Summary");
        var parseResult = new NewsBriefParseResult(item, false, ["No categories"]);

        // Act
        var markdown = _sut.Render(parseResult);

        // Assert
        Assert.That(markdown, Is.Empty);
    }

    #endregion
}
