using KanelBrief.Core.Agents;
using KanelBrief.Core.Models;
using KanelBrief.Core.Repositories;
using KanelBrief.Functions.Agents;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace KanelBrief.Functions.Tests.Agents;

[TestFixture]
[TestOf(typeof(WeeklySummaryAgent))]
public class WeeklySummaryAgentTests
{
    private Mock<IWeeklySummaryAnalyzer> _analyzer = null!;
    private Mock<IAgentRunRepository> _repository = null!;
    private TestTimeProvider _time = null!;
    private WeeklySummaryAgent _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _analyzer = new Mock<IWeeklySummaryAnalyzer>();
        _repository = new Mock<IAgentRunRepository>();
        _time = new TestTimeProvider(new DateTimeOffset(2026, 4, 6, 9, 0, 0, TimeSpan.Zero));
        _sut = new WeeklySummaryAgent(
            NullLogger<WeeklySummaryAgent>.Instance,
            _analyzer.Object,
            _repository.Object,
            _time);
    }

    private static WeeklySummaryRequest SampleRequest() => new()
    {
        WeekStart = new DateTimeOffset(2026, 3, 30, 0, 0, 0, TimeSpan.Zero),
        WeekEnd = new DateTimeOffset(2026, 4, 5, 0, 0, 0, TimeSpan.Zero),
        DailyBriefRunIds = []
    };

    private static NewsBriefRun Brief(string runDate) => new()
    {
        RunDate = runDate,
        RunId = Guid.NewGuid().ToString(),
        Mood = "Mixed",
        Summary = "x"
    };

    private void SetupOneBriefPerDay()
    {
        var dates = new[]
        {
            "2026-03-30", "2026-03-31", "2026-04-01",
            "2026-04-02", "2026-04-03", "2026-04-04", "2026-04-05"
        };
        foreach (var d in dates)
        {
            _repository.Setup(r => r.GetNewsBriefRunsByDateAsync(d))
                .ReturnsAsync(new List<NewsBriefRun> { Brief(d) });
        }
    }

    [Test]
    public async Task ExecuteAsync_WhenBriefsExistAndAnalyzerSucceeds_SavesRun()
    {
        // Arrange
        SetupOneBriefPerDay();
        _analyzer
            .Setup(a => a.AnalyzeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                It.IsAny<IReadOnlyList<NewsBriefRun>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WeeklySummaryAnalysisResult
            {
                Mood = "RiskOff",
                Summary = "Defensive week",
                Themes = [new WeeklySummaryTheme { Category = "Yields", Confidence = ConfidenceLevel.High, Sentiment = MarketSentiment.RiskOff }]
            });

        WeeklySummaryRun? saved = null;
        _repository.Setup(r => r.SaveWeeklySummaryRunAsync(It.IsAny<WeeklySummaryRun>()))
            .Callback<WeeklySummaryRun>(r => saved = r)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.ExecuteAsync(SampleRequest());

        // Assert
        Assert.That(saved, Is.Not.Null);
        Assert.That(result, Is.SameAs(saved));
        Assert.That(result.NetMood, Is.EqualTo(MarketSentiment.RiskOff));
        Assert.That(result.MoodSummary, Is.EqualTo("Defensive week"));
        Assert.That(result.Themes, Has.Count.EqualTo(1));
    }

    [Test]
    public void ExecuteAsync_WhenNoBriefsForWeek_ThrowsAndDoesNotCallAnalyzerOrSave()
    {
        // Arrange
        _repository.Setup(r => r.GetNewsBriefRunsByDateAsync(It.IsAny<string>()))
            .ReturnsAsync(new List<NewsBriefRun>());

        // Act
        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await _sut.ExecuteAsync(SampleRequest()));

        // Assert
        Assert.That(ex!.Message, Does.Contain("No daily briefs"));
        _analyzer.VerifyNoOtherCalls();
        _repository.Verify(r => r.SaveWeeklySummaryRunAsync(It.IsAny<WeeklySummaryRun>()), Times.Never);
    }

    [Test]
    public void ExecuteAsync_WhenAnalyzerThrows_RethrowsAndDoesNotSave()
    {
        // Arrange
        SetupOneBriefPerDay();
        _analyzer
            .Setup(a => a.AnalyzeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                It.IsAny<IReadOnlyList<NewsBriefRun>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("parse fail"));

        // Act
        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await _sut.ExecuteAsync(SampleRequest()));

        // Assert
        Assert.That(ex!.Message, Is.EqualTo("parse fail"));
        _repository.Verify(r => r.SaveWeeklySummaryRunAsync(It.IsAny<WeeklySummaryRun>()), Times.Never);
    }

    [Test]
    public async Task ExecuteAsync_Always_FetchesFullDateRangeInclusive()
    {
        // Arrange
        SetupOneBriefPerDay();
        _analyzer
            .Setup(a => a.AnalyzeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                It.IsAny<IReadOnlyList<NewsBriefRun>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WeeklySummaryAnalysisResult());

        // Act
        await _sut.ExecuteAsync(SampleRequest());

        // Assert
        foreach (var d in new[] { "2026-03-30", "2026-03-31", "2026-04-01", "2026-04-02", "2026-04-03", "2026-04-04", "2026-04-05" })
            _repository.Verify(r => r.GetNewsBriefRunsByDateAsync(d), Times.Once);
    }
}
