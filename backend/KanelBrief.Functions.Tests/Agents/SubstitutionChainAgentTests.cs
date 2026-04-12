using KanelBrief.Core.Agents;
using KanelBrief.Core.Models;
using KanelBrief.Core.Repositories;
using KanelBrief.Functions.Agents;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace KanelBrief.Functions.Tests.Agents;

[TestFixture]
[TestOf(typeof(SubstitutionChainAgent))]
public class SubstitutionChainAgentTests
{
    private Mock<ISubstitutionChainAnalyzer> _analyzer = null!;
    private Mock<IAgentRunRepository> _repository = null!;
    private TestTimeProvider _time = null!;
    private SubstitutionChainAgent _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _analyzer = new Mock<ISubstitutionChainAnalyzer>();
        _repository = new Mock<IAgentRunRepository>();
        _time = new TestTimeProvider(new DateTimeOffset(2026, 4, 6, 9, 30, 0, TimeSpan.Zero));
        _sut = new SubstitutionChainAgent(
            NullLogger<SubstitutionChainAgent>.Instance,
            _analyzer.Object,
            _repository.Object,
            _time);
    }

    private static SubstitutionChainRequest SampleRequest() => new()
    {
        WeeklySummaryRunDate = "2026-04-06",
        WeeklySummaryRunId = "summary-123"
    };

    private static WeeklySummaryRun SampleSummary() => new()
    {
        RunDate = "2026-04-06",
        RunId = "summary-123",
        NetMood = MarketSentiment.Mixed,
        MoodSummary = "Mixed week"
    };

    [Test]
    public async Task ExecuteAsync_WhenSummaryExistsAndAnalyzerSucceeds_SavesRun()
    {
        // Arrange
        _repository.Setup(r => r.GetWeeklySummaryRunAsync("2026-04-06", "summary-123"))
            .ReturnsAsync(SampleSummary());
        _analyzer.Setup(a => a.AnalyzeAsync(It.IsAny<WeeklySummaryRun>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubstitutionChainAnalysisResult
            {
                Chains = [new RotationChain { CapitalFleeing = "Energy", FlowsToward = "Technology", Mechanism = "AI capex" }]
            });

        SubstitutionChainRun? saved = null;
        _repository.Setup(r => r.SaveSubstitutionChainRunAsync(It.IsAny<SubstitutionChainRun>()))
            .Callback<SubstitutionChainRun>(r => saved = r)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.ExecuteAsync(SampleRequest());

        // Assert
        Assert.That(saved, Is.Not.Null);
        Assert.That(result, Is.SameAs(saved));
        Assert.That(result.WeeklySummaryRunId, Is.EqualTo("summary-123"));
        Assert.That(result.Chains, Has.Count.EqualTo(1));
    }

    [Test]
    public void ExecuteAsync_WhenRequestHasEmptyIds_ThrowsArgumentExceptionAndDoesNotHitRepo()
    {
        // Arrange
        var emptyRequest = new SubstitutionChainRequest { WeeklySummaryRunDate = "", WeeklySummaryRunId = "" };

        // Act
        var ex = Assert.ThrowsAsync<ArgumentException>(async () => await _sut.ExecuteAsync(emptyRequest));

        // Assert
        Assert.That(ex!.Message, Does.Contain("required"));
        _repository.Verify(r => r.GetWeeklySummaryRunAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _repository.Verify(r => r.SaveSubstitutionChainRunAsync(It.IsAny<SubstitutionChainRun>()), Times.Never);
        _analyzer.VerifyNoOtherCalls();
    }

    [Test]
    public void ExecuteAsync_WhenSummaryNotFound_ThrowsAndDoesNotCallAnalyzerOrSave()
    {
        // Arrange
        _repository.Setup(r => r.GetWeeklySummaryRunAsync("2026-04-06", "summary-123"))
            .ReturnsAsync((WeeklySummaryRun?)null);

        // Act
        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await _sut.ExecuteAsync(SampleRequest()));

        // Assert
        Assert.That(ex!.Message, Does.Contain("not found"));
        _analyzer.VerifyNoOtherCalls();
        _repository.Verify(r => r.SaveSubstitutionChainRunAsync(It.IsAny<SubstitutionChainRun>()), Times.Never);
    }

    [Test]
    public void ExecuteAsync_WhenAnalyzerThrows_RethrowsAndDoesNotSave()
    {
        // Arrange
        _repository.Setup(r => r.GetWeeklySummaryRunAsync("2026-04-06", "summary-123"))
            .ReturnsAsync(SampleSummary());
        _analyzer.Setup(a => a.AnalyzeAsync(It.IsAny<WeeklySummaryRun>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("LLM timeout"));

        // Act
        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await _sut.ExecuteAsync(SampleRequest()));

        // Assert
        Assert.That(ex!.Message, Is.EqualTo("LLM timeout"));
        _repository.Verify(r => r.SaveSubstitutionChainRunAsync(It.IsAny<SubstitutionChainRun>()), Times.Never);
    }
}
