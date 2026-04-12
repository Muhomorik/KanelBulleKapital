using KanelBrief.Core.Agents;
using KanelBrief.Core.Models;
using KanelBrief.Core.Repositories;
using KanelBrief.Functions.Agents;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace KanelBrief.Functions.Tests.Agents;

[TestFixture]
[TestOf(typeof(OpportunityScanAgent))]
public class OpportunityScanAgentTests
{
    private Mock<IOpportunityScanAnalyzer> _analyzer = null!;
    private Mock<IAgentRunRepository> _repository = null!;
    private TestTimeProvider _time = null!;
    private OpportunityScanAgent _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _analyzer = new Mock<IOpportunityScanAnalyzer>();
        _repository = new Mock<IAgentRunRepository>();
        _time = new TestTimeProvider(new DateTimeOffset(2026, 4, 6, 10, 0, 0, TimeSpan.Zero));
        _sut = new OpportunityScanAgent(
            NullLogger<OpportunityScanAgent>.Instance,
            _analyzer.Object,
            _repository.Object,
            _time);
    }

    private static OpportunityScanRequest SampleRequest() => new()
    {
        SubstitutionChainRunDate = "2026-04-06",
        SubstitutionChainRunId = "chain-456"
    };

    private static SubstitutionChainRun SampleChain() => new()
    {
        RunDate = "2026-04-06",
        RunId = "chain-456",
        WeeklySummaryRunId = "summary-123",
        Chains = [new RotationChain { CapitalFleeing = "Energy", FlowsToward = "Technology" }]
    };

    [Test]
    public async Task ExecuteAsync_WhenChainExistsAndAnalyzerSucceeds_SavesRun()
    {
        // Arrange
        _repository.Setup(r => r.GetSubstitutionChainRunAsync("2026-04-06", "chain-456"))
            .ReturnsAsync(SampleChain());
        _analyzer.Setup(a => a.AnalyzeAsync(It.IsAny<SubstitutionChainRun>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OpportunityScanAnalysisResult
            {
                Targets = [new RotationTarget { Category = "NVDA", SignalStrength = SignalStrength.Strong, Rationale = "AI leader" }]
            });

        OpportunityScanRun? saved = null;
        _repository.Setup(r => r.SaveOpportunityScanRunAsync(It.IsAny<OpportunityScanRun>()))
            .Callback<OpportunityScanRun>(r => saved = r)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.ExecuteAsync(SampleRequest());

        // Assert
        Assert.That(saved, Is.Not.Null);
        Assert.That(result, Is.SameAs(saved));
        Assert.That(result.SubstitutionChainRunId, Is.EqualTo("chain-456"));
        Assert.That(result.Targets, Has.Count.EqualTo(1));
        Assert.That(result.Targets[0].SignalStrength, Is.EqualTo(SignalStrength.Strong));
    }

    [Test]
    public void ExecuteAsync_WhenRequestHasEmptyIds_ThrowsArgumentExceptionAndDoesNotHitRepo()
    {
        // Arrange
        var emptyRequest = new OpportunityScanRequest { SubstitutionChainRunDate = "", SubstitutionChainRunId = "" };

        // Act
        var ex = Assert.ThrowsAsync<ArgumentException>(async () => await _sut.ExecuteAsync(emptyRequest));

        // Assert
        Assert.That(ex!.Message, Does.Contain("required"));
        _repository.Verify(r => r.GetSubstitutionChainRunAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _repository.Verify(r => r.SaveOpportunityScanRunAsync(It.IsAny<OpportunityScanRun>()), Times.Never);
        _analyzer.VerifyNoOtherCalls();
    }

    [Test]
    public void ExecuteAsync_WhenChainNotFound_ThrowsAndDoesNotCallAnalyzerOrSave()
    {
        // Arrange
        _repository.Setup(r => r.GetSubstitutionChainRunAsync("2026-04-06", "chain-456"))
            .ReturnsAsync((SubstitutionChainRun?)null);

        // Act
        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await _sut.ExecuteAsync(SampleRequest()));

        // Assert
        Assert.That(ex!.Message, Does.Contain("not found"));
        _analyzer.VerifyNoOtherCalls();
        _repository.Verify(r => r.SaveOpportunityScanRunAsync(It.IsAny<OpportunityScanRun>()), Times.Never);
    }

    [Test]
    public void ExecuteAsync_WhenAnalyzerThrows_RethrowsAndDoesNotSave()
    {
        // Arrange
        _repository.Setup(r => r.GetSubstitutionChainRunAsync("2026-04-06", "chain-456"))
            .ReturnsAsync(SampleChain());
        _analyzer.Setup(a => a.AnalyzeAsync(It.IsAny<SubstitutionChainRun>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("bad json"));

        // Act
        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await _sut.ExecuteAsync(SampleRequest()));

        // Assert
        Assert.That(ex!.Message, Is.EqualTo("bad json"));
        _repository.Verify(r => r.SaveOpportunityScanRunAsync(It.IsAny<OpportunityScanRun>()), Times.Never);
    }
}
