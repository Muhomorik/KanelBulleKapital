using KanelBrief.Core.Agents;
using KanelBrief.Core.Models;
using KanelBrief.Core.Repositories;
using KanelBrief.Functions.Agents;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace KanelBrief.Functions.Tests.Agents;

[TestFixture]
[TestOf(typeof(NewsBriefAgent))]
public class NewsBriefAgentTests
{
    private Mock<INewsBriefAnalyzer> _analyzer = null!;
    private Mock<IAgentRunRepository> _repository = null!;
    private TestTimeProvider _time = null!;
    private NewsBriefAgent _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _analyzer = new Mock<INewsBriefAnalyzer>();
        _repository = new Mock<IAgentRunRepository>();
        _time = new TestTimeProvider(new DateTimeOffset(2026, 4, 10, 14, 30, 0, TimeSpan.Zero));
        _sut = new NewsBriefAgent(
            NullLogger<NewsBriefAgent>.Instance,
            _analyzer.Object,
            _repository.Object,
            _time);
    }

    private static List<NewsArticle> SampleArticles() =>
    [
        new() { Title = "Fed holds rates", Content = "Powell says ...", Category = "Monetary" },
        new() { Title = "AI capex surge", Content = "NVDA earnings ...", Category = "Technology" }
    ];

    [Test]
    public async Task ExecuteAsync_WhenAnalyzerSucceeds_SavesRunAndReturnsIt()
    {
        // Arrange
        _analyzer.Setup(a => a.AnalyzeAsync(It.IsAny<IReadOnlyList<NewsArticle>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NewsBriefAnalysisResult
            {
                Mood = "RiskOn",
                Summary = "Equities rally",
                Assessments = [new CategoryAssessment { Category = "Technology", Sentiment = MarketSentiment.RiskOn }]
            });

        NewsBriefRun? saved = null;
        _repository.Setup(r => r.SaveNewsBriefRunAsync(It.IsAny<NewsBriefRun>()))
            .Callback<NewsBriefRun>(r => saved = r)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.ExecuteAsync(SampleArticles());

        // Assert
        Assert.That(saved, Is.Not.Null);
        Assert.That(result, Is.SameAs(saved));
        Assert.That(result.Status, Is.EqualTo(RunStatus.Success));
        Assert.That(result.Mood, Is.EqualTo("RiskOn"));
        Assert.That(result.Summary, Is.EqualTo("Equities rally"));
        Assert.That(result.Assessments, Has.Count.EqualTo(1));
        Assert.That(result.RunDate, Is.EqualTo("2026-04-10"));
    }

    [Test]
    public void ExecuteAsync_WhenArticlesEmpty_ThrowsAndDoesNotCallAnalyzerOrSave()
    {
        // Arrange
        var emptyArticles = new List<NewsArticle>();

        // Act
        var ex = Assert.ThrowsAsync<ArgumentException>(async () => await _sut.ExecuteAsync(emptyArticles));

        // Assert
        Assert.That(ex!.Message, Does.Contain("At least one"));
        _analyzer.VerifyNoOtherCalls();
        _repository.Verify(r => r.SaveNewsBriefRunAsync(It.IsAny<NewsBriefRun>()), Times.Never);
    }

    [Test]
    public void ExecuteAsync_WhenAnalyzerThrows_RethrowsAndDoesNotSave()
    {
        // Arrange
        _analyzer.Setup(a => a.AnalyzeAsync(It.IsAny<IReadOnlyList<NewsArticle>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Foundry down"));

        // Act
        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await _sut.ExecuteAsync(SampleArticles()));

        // Assert
        Assert.That(ex!.Message, Is.EqualTo("Foundry down"));
        _repository.Verify(r => r.SaveNewsBriefRunAsync(It.IsAny<NewsBriefRun>()), Times.Never);
    }

    [Test]
    public async Task ExecuteAsync_Always_ComputesDurationFromTimeProvider()
    {
        // Arrange
        _analyzer.Setup(a => a.AnalyzeAsync(It.IsAny<IReadOnlyList<NewsArticle>>(), It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                _time.Advance(TimeSpan.FromSeconds(7));
                await Task.Yield();
                return new NewsBriefAnalysisResult();
            });

        NewsBriefRun? saved = null;
        _repository.Setup(r => r.SaveNewsBriefRunAsync(It.IsAny<NewsBriefRun>()))
            .Callback<NewsBriefRun>(r => saved = r)
            .Returns(Task.CompletedTask);

        // Act
        await _sut.ExecuteAsync(SampleArticles());

        // Assert
        Assert.That(saved!.DurationSeconds, Is.EqualTo(7.0).Within(0.001));
    }
}
