using KanelBrief.Core.Agents;
using KanelBrief.Core.Models;
using KanelBrief.Core.Pipelines;
using KanelBrief.Core.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace KanelBrief.Core.Tests.Pipelines;

[TestFixture]
[TestOf(typeof(NewsBriefPipeline))]
public class NewsBriefPipelineTests
{
    private Mock<IAgentRunRepository> _repository = null!;
    private Mock<INewsBriefAnalyzer> _analyzer = null!;
    private FakeTimeProvider _time = null!;
    private NewsBriefPipeline _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = new Mock<IAgentRunRepository>();
        _analyzer = new Mock<INewsBriefAnalyzer>();
        _time = new FakeTimeProvider(new DateTimeOffset(2026, 4, 7, 8, 0, 0, TimeSpan.Zero));
        _sut = new NewsBriefPipeline(
            NullLogger<NewsBriefPipeline>.Instance,
            _repository.Object,
            _analyzer.Object,
            _time);
    }

    [Test]
    public async Task ExecuteAsync_WhenAnalyzerSucceeds_SavesRunWithMoodSummaryAssessments()
    {
        // Arrange
        var analysis = new NewsBriefAnalysisResult
        {
            Mood = "RiskOn",
            Summary = "Equities rally",
            Assessments =
            [
                new CategoryAssessment { Category = "Technology", Sentiment = MarketSentiment.RiskOn },
                new CategoryAssessment { Category = "Energy", Sentiment = MarketSentiment.Mixed }
            ]
        };
        _analyzer.Setup(a => a.AnalyzeAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(analysis);

        NewsBriefRun? saved = null;
        _repository.Setup(r => r.SaveNewsBriefRunAsync(It.IsAny<NewsBriefRun>()))
            .Callback<NewsBriefRun>(r => saved = r)
            .Returns(Task.CompletedTask);

        // Act
        await _sut.ExecuteAsync();

        // Assert
        Assert.That(saved, Is.Not.Null);
        Assert.That(saved!.Mood, Is.EqualTo("RiskOn"));
        Assert.That(saved.Summary, Is.EqualTo("Equities rally"));
        Assert.That(saved.Assessments, Has.Count.EqualTo(2));
        Assert.That(saved.Status, Is.EqualTo(RunStatus.Success));
    }

    [Test]
    public async Task ExecuteAsync_WhenAnalyzerSucceeds_SetsStatusToSuccess()
    {
        // Arrange
        _analyzer.Setup(a => a.AnalyzeAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NewsBriefAnalysisResult { Mood = "Mixed", Summary = "Flat" });

        NewsBriefRun? saved = null;
        _repository.Setup(r => r.SaveNewsBriefRunAsync(It.IsAny<NewsBriefRun>()))
            .Callback<NewsBriefRun>(r => saved = r)
            .Returns(Task.CompletedTask);

        // Act
        await _sut.ExecuteAsync();

        // Assert
        Assert.That(saved!.Status, Is.EqualTo(RunStatus.Success));
    }

    [Test]
    public void ExecuteAsync_WhenAnalyzerThrows_RethrowsAndDoesNotSave()
    {
        // Arrange
        _analyzer.Setup(a => a.AnalyzeAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Foundry unavailable"));

        // Act
        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await _sut.ExecuteAsync());

        // Assert
        Assert.That(ex!.Message, Is.EqualTo("Foundry unavailable"));
        _repository.Verify(r => r.SaveNewsBriefRunAsync(It.IsAny<NewsBriefRun>()), Times.Never);
    }

    [Test]
    public async Task ExecuteAsync_Always_PopulatesRunDateFromTimeProvider()
    {
        // Arrange
        _time.Set(new DateTimeOffset(2027, 1, 15, 8, 0, 0, TimeSpan.Zero));
        _analyzer.Setup(a => a.AnalyzeAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NewsBriefAnalysisResult());

        NewsBriefRun? saved = null;
        _repository.Setup(r => r.SaveNewsBriefRunAsync(It.IsAny<NewsBriefRun>()))
            .Callback<NewsBriefRun>(r => saved = r)
            .Returns(Task.CompletedTask);

        // Act
        await _sut.ExecuteAsync();

        // Assert
        Assert.That(saved!.RunDate, Is.EqualTo("2027-01-15"));
        Assert.That(saved.CreatedAt, Is.EqualTo(new DateTimeOffset(2027, 1, 15, 8, 0, 0, TimeSpan.Zero)));
    }

    [Test]
    public async Task ExecuteAsync_Always_ComputesDurationSecondsFromTimeProvider()
    {
        // Arrange
        _analyzer.Setup(a => a.AnalyzeAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                _time.Advance(TimeSpan.FromSeconds(12));
                await Task.Yield();
                return new NewsBriefAnalysisResult();
            });

        NewsBriefRun? saved = null;
        _repository.Setup(r => r.SaveNewsBriefRunAsync(It.IsAny<NewsBriefRun>()))
            .Callback<NewsBriefRun>(r => saved = r)
            .Returns(Task.CompletedTask);

        // Act
        await _sut.ExecuteAsync();

        // Assert
        Assert.That(saved!.DurationSeconds, Is.EqualTo(12.0).Within(0.001));
    }

    [Test]
    public async Task ExecuteAsync_Always_CallsRepositorySaveExactlyOnce()
    {
        // Arrange
        _analyzer.Setup(a => a.AnalyzeAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NewsBriefAnalysisResult());

        // Act
        await _sut.ExecuteAsync();

        // Assert
        _repository.Verify(r => r.SaveNewsBriefRunAsync(It.IsAny<NewsBriefRun>()), Times.Once);
    }

    [Test]
    public async Task ExecuteAsync_Always_AssignsUniqueRunId()
    {
        // Arrange
        _analyzer.Setup(a => a.AnalyzeAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NewsBriefAnalysisResult());

        var seenIds = new List<string>();
        _repository.Setup(r => r.SaveNewsBriefRunAsync(It.IsAny<NewsBriefRun>()))
            .Callback<NewsBriefRun>(r => seenIds.Add(r.RunId))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.ExecuteAsync();
        await _sut.ExecuteAsync();

        // Assert
        Assert.That(seenIds, Has.Count.EqualTo(2));
        Assert.That(seenIds[0], Is.Not.EqualTo(seenIds[1]));
        Assert.That(Guid.TryParse(seenIds[0], out _), Is.True);
    }
}
