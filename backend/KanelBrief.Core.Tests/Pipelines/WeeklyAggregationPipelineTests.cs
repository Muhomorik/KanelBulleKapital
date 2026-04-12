using KanelBrief.Core.Agents;
using KanelBrief.Core.Models;
using KanelBrief.Core.Pipelines;
using KanelBrief.Core.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace KanelBrief.Core.Tests.Pipelines;

[TestFixture]
[TestOf(typeof(WeeklyAggregationPipeline))]
public class WeeklyAggregationPipelineTests
{
    private Mock<IAgentRunRepository> _repository = null!;
    private Mock<IWeeklySummaryAnalyzer> _weeklySummaryAnalyzer = null!;
    private Mock<ISubstitutionChainAnalyzer> _substitutionChainAnalyzer = null!;
    private Mock<IOpportunityScanAnalyzer> _opportunityScanAnalyzer = null!;
    private FakeTimeProvider _time = null!;
    private WeeklyAggregationPipeline _sut = null!;

    // Fixed "now" = Monday April 6, 2026, 09:00 UTC.
    // Previous week boundary: Monday March 30 → exclusive Monday April 6 (week Mar 30 – Apr 5 inclusive).
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 6, 9, 0, 0, TimeSpan.Zero);

    [SetUp]
    public void SetUp()
    {
        _repository = new Mock<IAgentRunRepository>();
        _weeklySummaryAnalyzer = new Mock<IWeeklySummaryAnalyzer>();
        _substitutionChainAnalyzer = new Mock<ISubstitutionChainAnalyzer>();
        _opportunityScanAnalyzer = new Mock<IOpportunityScanAnalyzer>();
        _time = new FakeTimeProvider(FixedNow);
        _sut = new WeeklyAggregationPipeline(
            NullLogger<WeeklyAggregationPipeline>.Instance,
            _repository.Object,
            _weeklySummaryAnalyzer.Object,
            _substitutionChainAnalyzer.Object,
            _opportunityScanAnalyzer.Object,
            _time);
    }

    private static NewsBriefRun SampleBrief(string runDate) => new()
    {
        RunDate = runDate,
        RunId = Guid.NewGuid().ToString(),
        Mood = "Mixed",
        Summary = "Uneventful session",
        Assessments = []
    };

    private void SetupBriefsForPreviousWeek(int briefsPerDay = 1)
    {
        var weekDates = new[]
        {
            "2026-03-30", "2026-03-31", "2026-04-01",
            "2026-04-02", "2026-04-03", "2026-04-04", "2026-04-05"
        };
        foreach (var date in weekDates)
        {
            var list = Enumerable.Range(0, briefsPerDay).Select(_ => SampleBrief(date)).ToList();
            _repository.Setup(r => r.GetNewsBriefRunsByDateAsync(date)).ReturnsAsync(list);
        }
    }

    private void SetupAllAnalyzersSucceed()
    {
        _weeklySummaryAnalyzer
            .Setup(a => a.AnalyzeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                It.IsAny<IReadOnlyList<NewsBriefRun>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WeeklySummaryAnalysisResult
            {
                Mood = "RiskOn",
                Summary = "Risk appetite returning",
                Themes =
                [
                    new WeeklySummaryTheme { Category = "AI boom", Confidence = ConfidenceLevel.High, Sentiment = MarketSentiment.RiskOn }
                ]
            });

        _substitutionChainAnalyzer
            .Setup(a => a.AnalyzeAsync(It.IsAny<WeeklySummaryRun>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubstitutionChainAnalysisResult
            {
                Chains =
                [
                    new RotationChain { CapitalFleeing = "Energy", FlowsToward = "Technology", Mechanism = "AI capex" }
                ]
            });

        _opportunityScanAnalyzer
            .Setup(a => a.AnalyzeAsync(It.IsAny<SubstitutionChainRun>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OpportunityScanAnalysisResult
            {
                Targets =
                [
                    new RotationTarget { Category = "NVDA", SignalStrength = SignalStrength.Strong, Rationale = "AI leader" }
                ]
            });
    }

    [Test]
    public async Task ExecuteAsync_WhenNoDailyBriefs_SkipsAggregationAndSavesNothing()
    {
        // Arrange
        _repository.Setup(r => r.GetNewsBriefRunsByDateAsync(It.IsAny<string>()))
            .ReturnsAsync(new List<NewsBriefRun>());

        // Act
        await _sut.ExecuteAsync();

        // Assert
        _repository.Verify(r => r.SaveWeeklySummaryRunAsync(It.IsAny<WeeklySummaryRun>()), Times.Never);
        _repository.Verify(r => r.SaveSubstitutionChainRunAsync(It.IsAny<SubstitutionChainRun>()), Times.Never);
        _repository.Verify(r => r.SaveOpportunityScanRunAsync(It.IsAny<OpportunityScanRun>()), Times.Never);
        _weeklySummaryAnalyzer.VerifyNoOtherCalls();
        _substitutionChainAnalyzer.VerifyNoOtherCalls();
        _opportunityScanAnalyzer.VerifyNoOtherCalls();
    }

    [Test]
    public async Task ExecuteAsync_WhenBriefsExist_ChainsAllThreeAnalyzersInOrder()
    {
        // Arrange
        SetupBriefsForPreviousWeek();
        SetupAllAnalyzersSucceed();

        var callOrder = new List<string>();
        _weeklySummaryAnalyzer
            .Setup(a => a.AnalyzeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                It.IsAny<IReadOnlyList<NewsBriefRun>>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("weekly"))
            .ReturnsAsync(new WeeklySummaryAnalysisResult { Mood = "Mixed", Summary = "x" });
        _substitutionChainAnalyzer
            .Setup(a => a.AnalyzeAsync(It.IsAny<WeeklySummaryRun>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("chain"))
            .ReturnsAsync(new SubstitutionChainAnalysisResult());
        _opportunityScanAnalyzer
            .Setup(a => a.AnalyzeAsync(It.IsAny<SubstitutionChainRun>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("scan"))
            .ReturnsAsync(new OpportunityScanAnalysisResult());

        // Act
        await _sut.ExecuteAsync();

        // Assert
        Assert.That(callOrder, Is.EqualTo(new[] { "weekly", "chain", "scan" }));
    }

    [Test]
    public void ExecuteAsync_WhenWeeklySummaryAnalyzerFails_RethrowsAndDoesNotRunDownstreamSteps()
    {
        // Arrange
        SetupBriefsForPreviousWeek();
        _weeklySummaryAnalyzer
            .Setup(a => a.AnalyzeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                It.IsAny<IReadOnlyList<NewsBriefRun>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("LLM down"));

        // Act
        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await _sut.ExecuteAsync());

        // Assert
        Assert.That(ex!.Message, Is.EqualTo("LLM down"));
        _repository.Verify(r => r.SaveWeeklySummaryRunAsync(It.IsAny<WeeklySummaryRun>()), Times.Never);
        _repository.Verify(r => r.SaveSubstitutionChainRunAsync(It.IsAny<SubstitutionChainRun>()), Times.Never);
        _repository.Verify(r => r.SaveOpportunityScanRunAsync(It.IsAny<OpportunityScanRun>()), Times.Never);
        _substitutionChainAnalyzer.Verify(a => a.AnalyzeAsync(It.IsAny<WeeklySummaryRun>(), It.IsAny<CancellationToken>()), Times.Never);
        _opportunityScanAnalyzer.Verify(a => a.AnalyzeAsync(It.IsAny<SubstitutionChainRun>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void ExecuteAsync_WhenSubstitutionChainFails_RethrowsAndDoesNotRunOpportunityScan()
    {
        // Arrange
        SetupBriefsForPreviousWeek();
        SetupAllAnalyzersSucceed();
        _substitutionChainAnalyzer
            .Setup(a => a.AnalyzeAsync(It.IsAny<WeeklySummaryRun>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("parse fail"));

        // Act
        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await _sut.ExecuteAsync());

        // Assert
        Assert.That(ex!.Message, Is.EqualTo("parse fail"));
        // Weekly Summary completed before the chain failure, so it was saved.
        _repository.Verify(r => r.SaveWeeklySummaryRunAsync(It.IsAny<WeeklySummaryRun>()), Times.Once);
        // Substitution Chain threw before save, so nothing was persisted.
        _repository.Verify(r => r.SaveSubstitutionChainRunAsync(It.IsAny<SubstitutionChainRun>()), Times.Never);
        // Opportunity Scan must not run at all.
        _repository.Verify(r => r.SaveOpportunityScanRunAsync(It.IsAny<OpportunityScanRun>()), Times.Never);
        _opportunityScanAnalyzer.Verify(a => a.AnalyzeAsync(It.IsAny<SubstitutionChainRun>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void ExecuteAsync_WhenOpportunityScanFails_RethrowsButPriorStepsRemainSaved()
    {
        // Arrange
        SetupBriefsForPreviousWeek();
        SetupAllAnalyzersSucceed();
        _opportunityScanAnalyzer
            .Setup(a => a.AnalyzeAsync(It.IsAny<SubstitutionChainRun>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("scan parse fail"));

        // Act
        Assert.ThrowsAsync<InvalidOperationException>(async () => await _sut.ExecuteAsync());

        // Assert
        _repository.Verify(r => r.SaveWeeklySummaryRunAsync(It.IsAny<WeeklySummaryRun>()), Times.Once);
        _repository.Verify(r => r.SaveSubstitutionChainRunAsync(It.IsAny<SubstitutionChainRun>()), Times.Once);
        _repository.Verify(r => r.SaveOpportunityScanRunAsync(It.IsAny<OpportunityScanRun>()), Times.Never);
    }

    [Test]
    public async Task ExecuteAsync_Always_FetchesPreviousWeekMondayToSunday()
    {
        // Arrange
        SetupBriefsForPreviousWeek();
        SetupAllAnalyzersSucceed();

        // Act
        await _sut.ExecuteAsync();

        // Assert
        // Previous week (Mon Mar 30 .. Sun Apr 5 inclusive) — exclusive upper bound is Apr 6.
        var expectedDates = new[]
        {
            "2026-03-30", "2026-03-31", "2026-04-01",
            "2026-04-02", "2026-04-03", "2026-04-04", "2026-04-05"
        };
        foreach (var d in expectedDates)
            _repository.Verify(r => r.GetNewsBriefRunsByDateAsync(d), Times.Once);

        // Should not fetch the boundary Monday or beyond.
        _repository.Verify(r => r.GetNewsBriefRunsByDateAsync("2026-04-06"), Times.Never);
        _repository.Verify(r => r.GetNewsBriefRunsByDateAsync("2026-03-29"), Times.Never);
    }

    [Test]
    public async Task ExecuteAsync_Always_PassesWeeklySummaryRunIdToSubstitutionChain()
    {
        // Arrange
        SetupBriefsForPreviousWeek();
        SetupAllAnalyzersSucceed();

        WeeklySummaryRun? savedSummary = null;
        _repository.Setup(r => r.SaveWeeklySummaryRunAsync(It.IsAny<WeeklySummaryRun>()))
            .Callback<WeeklySummaryRun>(r => savedSummary = r)
            .Returns(Task.CompletedTask);

        SubstitutionChainRun? savedChain = null;
        _repository.Setup(r => r.SaveSubstitutionChainRunAsync(It.IsAny<SubstitutionChainRun>()))
            .Callback<SubstitutionChainRun>(r => savedChain = r)
            .Returns(Task.CompletedTask);

        // Act
        await _sut.ExecuteAsync();

        // Assert
        Assert.That(savedSummary, Is.Not.Null);
        Assert.That(savedChain, Is.Not.Null);
        Assert.That(savedChain!.WeeklySummaryRunId, Is.EqualTo(savedSummary!.RunId));
    }

    [Test]
    public async Task ExecuteAsync_Always_PassesSubstitutionChainRunIdToOpportunityScan()
    {
        // Arrange
        SetupBriefsForPreviousWeek();
        SetupAllAnalyzersSucceed();

        SubstitutionChainRun? savedChain = null;
        _repository.Setup(r => r.SaveSubstitutionChainRunAsync(It.IsAny<SubstitutionChainRun>()))
            .Callback<SubstitutionChainRun>(r => savedChain = r)
            .Returns(Task.CompletedTask);

        OpportunityScanRun? savedScan = null;
        _repository.Setup(r => r.SaveOpportunityScanRunAsync(It.IsAny<OpportunityScanRun>()))
            .Callback<OpportunityScanRun>(r => savedScan = r)
            .Returns(Task.CompletedTask);

        // Act
        await _sut.ExecuteAsync();

        // Assert
        Assert.That(savedChain, Is.Not.Null);
        Assert.That(savedScan, Is.Not.Null);
        Assert.That(savedScan!.SubstitutionChainRunId, Is.EqualTo(savedChain!.RunId));
    }

    [Test]
    public async Task ExecuteAsync_WhenAnalyzerSucceeds_MapsWeeklySummaryNetMoodViaParser()
    {
        // Arrange
        SetupBriefsForPreviousWeek();
        _weeklySummaryAnalyzer
            .Setup(a => a.AnalyzeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                It.IsAny<IReadOnlyList<NewsBriefRun>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WeeklySummaryAnalysisResult { Mood = "RiskOff", Summary = "Defensive" });
        _substitutionChainAnalyzer
            .Setup(a => a.AnalyzeAsync(It.IsAny<WeeklySummaryRun>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubstitutionChainAnalysisResult());
        _opportunityScanAnalyzer
            .Setup(a => a.AnalyzeAsync(It.IsAny<SubstitutionChainRun>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OpportunityScanAnalysisResult());

        WeeklySummaryRun? savedSummary = null;
        _repository.Setup(r => r.SaveWeeklySummaryRunAsync(It.IsAny<WeeklySummaryRun>()))
            .Callback<WeeklySummaryRun>(r => savedSummary = r)
            .Returns(Task.CompletedTask);

        // Act
        await _sut.ExecuteAsync();

        // Assert
        Assert.That(savedSummary!.NetMood, Is.EqualTo(MarketSentiment.RiskOff));
        Assert.That(savedSummary.MoodSummary, Is.EqualTo("Defensive"));
    }
}
