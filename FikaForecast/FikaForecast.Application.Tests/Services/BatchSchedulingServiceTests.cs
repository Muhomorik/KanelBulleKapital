using System.Reactive.Concurrency;
using AutoFixture;
using AutoFixture.AutoMoq;
using FikaForecast.Application.DTOs;
using FikaForecast.Application.Interfaces;
using FikaForecast.Application.Services;
using FikaForecast.Domain.Entities;
using FikaForecast.Domain.ValueObjects;
using FluentResults;
using Microsoft.Reactive.Testing;
using Moq;
using NLog;

namespace FikaForecast.Application.Tests.Services;

[TestFixture]
[TestOf(typeof(BatchSchedulingService))]
public class BatchSchedulingServiceTests
{
    private IFixture _fixture;
    private TestScheduler _scheduler;
    private Mock<ILogger> _loggerMock;
    private Mock<IBriefComparisonService> _comparisonServiceMock;
    private BatchSchedulingService _sut;

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture().Customize(new AutoMoqCustomization());
        _scheduler = new TestScheduler();
        _fixture.Register<IScheduler>(() => _scheduler);
        _loggerMock = _fixture.Freeze<Mock<ILogger>>();
        _comparisonServiceMock = _fixture.Freeze<Mock<IBriefComparisonService>>();
        _sut = _fixture.Create<BatchSchedulingService>();
    }

    #region BuildDaySlots

    [Test]
    [TestOf(nameof(BatchSchedulingService.BuildDaySlots))]
    public void BuildDaySlots_FourHourInterval_ReturnsSixSlots()
    {
        // Arrange
        var interval = TimeSpan.FromHours(4);

        // Act
        var result = _sut.BuildDaySlots(interval);

        // Assert
        Assert.That(result, Has.Count.EqualTo(6));
    }

    [Test]
    [TestOf(nameof(BatchSchedulingService.BuildDaySlots))]
    public void BuildDaySlots_FourHourInterval_ReturnsCorrectPlannedTimes()
    {
        // Arrange
        var interval = TimeSpan.FromHours(4);

        // Act
        var result = _sut.BuildDaySlots(interval);

        // Assert
        var expectedTimes = new[]
        {
            new TimeOnly(0, 0),
            new TimeOnly(4, 0),
            new TimeOnly(8, 0),
            new TimeOnly(12, 0),
            new TimeOnly(16, 0),
            new TimeOnly(20, 0)
        };
        var actualTimes = result.Select(s => s.PlannedTime).ToArray();
        Assert.That(actualTimes, Is.EqualTo(expectedTimes));
    }

    [Test]
    [TestOf(nameof(BatchSchedulingService.BuildDaySlots))]
    public void BuildDaySlots_SixHourInterval_ReturnsFourSlots()
    {
        // Arrange
        var interval = TimeSpan.FromHours(6);

        // Act
        var result = _sut.BuildDaySlots(interval);

        // Assert
        Assert.That(result, Has.Count.EqualTo(4));
    }

    [Test]
    [TestOf(nameof(BatchSchedulingService.BuildDaySlots))]
    public void BuildDaySlots_OneHourInterval_ReturnsTwentyFourSlots()
    {
        // Arrange
        var interval = TimeSpan.FromHours(1);

        // Act
        var result = _sut.BuildDaySlots(interval);

        // Assert
        Assert.That(result, Has.Count.EqualTo(24));
    }

    [Test]
    [TestOf(nameof(BatchSchedulingService.BuildDaySlots))]
    public void BuildDaySlots_PastSlotsAppearBeforeFutureSlots()
    {
        // Arrange
        var interval = TimeSpan.FromHours(4);

        // Act
        var result = _sut.BuildDaySlots(interval);

        // Assert — chronological order means past slots come first
        var pastSlots = result.Where(s => s.IsPast).ToList();
        var futureSlots = result.Where(s => !s.IsPast).ToList();

        if (pastSlots.Count > 0 && futureSlots.Count > 0)
        {
            Assert.That(pastSlots.Last().PlannedTime, Is.LessThan(futureSlots.First().PlannedTime));
        }
    }

    #endregion

    #region ExecuteSlotAsync

    [Test]
    [TestOf(nameof(BatchSchedulingService.ExecuteSlotAsync))]
    public async Task ExecuteSlotAsync_AllModelsSucceed_ReturnsCorrectCounts()
    {
        // Arrange
        var models = CreateModels(3);
        var prompt = _fixture.Create<AgentPrompt>();
        var runs = models.Select(m => CreateSuccessfulRun(m, prompt)).ToList();

        _comparisonServiceMock
            .Setup(x => x.CompareSequentiallyAsync(
                models, prompt, It.IsAny<IProgress<BatchProgress>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(runs.Select(Result.Ok).ToList<Result<NewsBriefRun>>());

        // Act
        var result = await _sut.ExecuteSlotAsync(models, prompt, null, CancellationToken.None);

        // Assert
        Assert.That(result.SuccessCount, Is.EqualTo(3));
        Assert.That(result.FailureCount, Is.EqualTo(0));
        Assert.That(result.HasFailures, Is.False);
    }

    [Test]
    [TestOf(nameof(BatchSchedulingService.ExecuteSlotAsync))]
    public async Task ExecuteSlotAsync_AllModelsSucceed_SumsTotalTokens()
    {
        // Arrange
        var models = CreateModels(2);
        var prompt = _fixture.Create<AgentPrompt>();
        var run1 = CreateSuccessfulRun(models[0], prompt);
        var run2 = CreateSuccessfulRun(models[1], prompt);
        var expectedTokens = run1.TotalTokens + run2.TotalTokens;

        var results = new List<Result<NewsBriefRun>> { Result.Ok(run1), Result.Ok(run2) };

        _comparisonServiceMock
            .Setup(x => x.CompareSequentiallyAsync(
                models, prompt, It.IsAny<IProgress<BatchProgress>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(results);

        // Act
        var result = await _sut.ExecuteSlotAsync(models, prompt, null, CancellationToken.None);

        // Assert
        Assert.That(result.TotalTokens, Is.EqualTo(expectedTokens));
    }

    [Test]
    [TestOf(nameof(BatchSchedulingService.ExecuteSlotAsync))]
    public async Task ExecuteSlotAsync_SomeModelsFail_ReturnsCorrectCounts()
    {
        // Arrange
        var models = CreateModels(3);
        var prompt = _fixture.Create<AgentPrompt>();

        var results = new List<Result<NewsBriefRun>>
        {
            Result.Ok(CreateSuccessfulRun(models[0], prompt)),
            Result.Fail<NewsBriefRun>("Model failed"),
            Result.Fail<NewsBriefRun>("Model failed")
        };

        _comparisonServiceMock
            .Setup(x => x.CompareSequentiallyAsync(
                models, prompt, It.IsAny<IProgress<BatchProgress>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(results);

        // Act
        var result = await _sut.ExecuteSlotAsync(models, prompt, null, CancellationToken.None);

        // Assert
        Assert.That(result.SuccessCount, Is.EqualTo(1));
        Assert.That(result.FailureCount, Is.EqualTo(2));
        Assert.That(result.HasFailures, Is.True);
    }

    [Test]
    [TestOf(nameof(BatchSchedulingService.ExecuteSlotAsync))]
    public async Task ExecuteSlotAsync_SomeModelsFail_OnlySumsSuccessfulTokens()
    {
        // Arrange
        var models = CreateModels(2);
        var prompt = _fixture.Create<AgentPrompt>();
        var successRun = CreateSuccessfulRun(models[0], prompt);

        var results = new List<Result<NewsBriefRun>>
        {
            Result.Ok(successRun),
            Result.Fail<NewsBriefRun>("Model failed")
        };

        _comparisonServiceMock
            .Setup(x => x.CompareSequentiallyAsync(
                models, prompt, It.IsAny<IProgress<BatchProgress>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(results);

        // Act
        var result = await _sut.ExecuteSlotAsync(models, prompt, null, CancellationToken.None);

        // Assert
        Assert.That(result.TotalTokens, Is.EqualTo(successRun.TotalTokens));
    }

    [Test]
    [TestOf(nameof(BatchSchedulingService.ExecuteSlotAsync))]
    public async Task ExecuteSlotAsync_ForwardsProgressToComparisonService()
    {
        // Arrange
        var models = CreateModels(1);
        var prompt = _fixture.Create<AgentPrompt>();
        var progress = new Mock<IProgress<BatchProgress>>();
        var run = CreateSuccessfulRun(models[0], prompt);

        _comparisonServiceMock
            .Setup(x => x.CompareSequentiallyAsync(
                models, prompt, progress.Object, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Result<NewsBriefRun>> { Result.Ok(run) });

        // Act
        await _sut.ExecuteSlotAsync(models, prompt, progress.Object, CancellationToken.None);

        // Assert
        _comparisonServiceMock.Verify(
            x => x.CompareSequentiallyAsync(models, prompt, progress.Object, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    [TestOf(nameof(BatchSchedulingService.ExecuteSlotAsync))]
    public async Task ExecuteSlotAsync_ForwardsCancellationToken()
    {
        // Arrange
        var models = CreateModels(1);
        var prompt = _fixture.Create<AgentPrompt>();
        using var cts = new CancellationTokenSource();
        var run = CreateSuccessfulRun(models[0], prompt);

        _comparisonServiceMock
            .Setup(x => x.CompareSequentiallyAsync(
                models, prompt, It.IsAny<IProgress<BatchProgress>>(), cts.Token))
            .ReturnsAsync(new List<Result<NewsBriefRun>> { Result.Ok(run) });

        // Act
        await _sut.ExecuteSlotAsync(models, prompt, null, cts.Token);

        // Assert
        _comparisonServiceMock.Verify(
            x => x.CompareSequentiallyAsync(models, prompt, It.IsAny<IProgress<BatchProgress>>(), cts.Token),
            Times.Once);
    }

    [Test]
    [TestOf(nameof(BatchSchedulingService.ExecuteSlotAsync))]
    public async Task ExecuteSlotAsync_ReturnsDurationGreaterThanOrEqualToZero()
    {
        // Arrange
        var models = CreateModels(1);
        var prompt = _fixture.Create<AgentPrompt>();
        var run = CreateSuccessfulRun(models[0], prompt);

        _comparisonServiceMock
            .Setup(x => x.CompareSequentiallyAsync(
                models, prompt, It.IsAny<IProgress<BatchProgress>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Result<NewsBriefRun>> { Result.Ok(run) });

        // Act
        var result = await _sut.ExecuteSlotAsync(models, prompt, null, CancellationToken.None);

        // Assert
        Assert.That(result.Duration, Is.GreaterThanOrEqualTo(TimeSpan.Zero));
    }

    #endregion

    #region CreateTimer

    [Test]
    [TestOf(nameof(BatchSchedulingService.CreateTimer))]
    public void CreateTimer_AdvancePastDelay_Fires()
    {
        // Arrange
        var delay = TimeSpan.FromHours(4);
        var fired = false;
        _sut.CreateTimer(delay).Subscribe(_ => fired = true);

        // Act
        _scheduler.AdvanceBy(delay.Ticks);

        // Assert
        Assert.That(fired, Is.True);
    }

    [Test]
    [TestOf(nameof(BatchSchedulingService.CreateTimer))]
    public void CreateTimer_BeforeDelay_DoesNotFire()
    {
        // Arrange
        var delay = TimeSpan.FromHours(4);
        var fired = false;
        _sut.CreateTimer(delay).Subscribe(_ => fired = true);

        // Act — advance to 1 tick before the delay
        _scheduler.AdvanceBy(delay.Ticks - 1);

        // Assert
        Assert.That(fired, Is.False);
    }

    [Test]
    [TestOf(nameof(BatchSchedulingService.CreateTimer))]
    public void CreateTimer_Disposed_DoesNotFire()
    {
        // Arrange
        var delay = TimeSpan.FromHours(4);
        var fired = false;
        var subscription = _sut.CreateTimer(delay).Subscribe(_ => fired = true);

        // Act
        subscription.Dispose();
        _scheduler.AdvanceBy(delay.Ticks);

        // Assert
        Assert.That(fired, Is.False);
    }

    #endregion

    #region CalculateWeeklyDelay

    [Test]
    [TestOf(nameof(BatchSchedulingService.CalculateWeeklyDelay))]
    public void CalculateWeeklyDelay_ThursdayNotYetPassed_ReturnsDelayToThisThursday()
    {
        // Arrange — Monday 10:00 UTC, target Thursday 22:00
        var utcNow = new DateTime(2026, 4, 6, 10, 0, 0, DateTimeKind.Utc); // Monday

        // Act
        var delay = _sut.CalculateWeeklyDelay(DayOfWeek.Thursday, new TimeOnly(22, 0), utcNow);

        // Assert — Thursday 22:00 is 3 days + 12 hours away
        var expected = TimeSpan.FromHours(3 * 24 + 12);
        Assert.That(delay, Is.EqualTo(expected));
    }

    [Test]
    [TestOf(nameof(BatchSchedulingService.CalculateWeeklyDelay))]
    public void CalculateWeeklyDelay_ThursdayAlreadyPassed_ReturnsDelayToNextThursday()
    {
        // Arrange — Friday 08:00 UTC, target Thursday 22:00
        var utcNow = new DateTime(2026, 4, 10, 8, 0, 0, DateTimeKind.Utc); // Friday

        // Act
        var delay = _sut.CalculateWeeklyDelay(DayOfWeek.Thursday, new TimeOnly(22, 0), utcNow);

        // Assert — next Thursday 22:00 is 6 days + 14 hours away
        var expected = TimeSpan.FromHours(6 * 24 + 14);
        Assert.That(delay, Is.EqualTo(expected));
    }

    [Test]
    [TestOf(nameof(BatchSchedulingService.CalculateWeeklyDelay))]
    public void CalculateWeeklyDelay_ExactlyOnTargetTime_ReturnsSevenDays()
    {
        // Arrange — Thursday 22:00 exactly
        var utcNow = new DateTime(2026, 4, 9, 22, 0, 0, DateTimeKind.Utc); // Thursday

        // Act
        var delay = _sut.CalculateWeeklyDelay(DayOfWeek.Thursday, new TimeOnly(22, 0), utcNow);

        // Assert — should skip to next week
        Assert.That(delay, Is.EqualTo(TimeSpan.FromDays(7)));
    }

    [Test]
    [TestOf(nameof(BatchSchedulingService.CalculateWeeklyDelay))]
    public void CalculateWeeklyDelay_SameDayBeforeTargetTime_ReturnsDelayToday()
    {
        // Arrange — Thursday 10:00 UTC, target Thursday 22:00
        var utcNow = new DateTime(2026, 4, 9, 10, 0, 0, DateTimeKind.Utc); // Thursday

        // Act
        var delay = _sut.CalculateWeeklyDelay(DayOfWeek.Thursday, new TimeOnly(22, 0), utcNow);

        // Assert — 12 hours away
        Assert.That(delay, Is.EqualTo(TimeSpan.FromHours(12)));
    }

    [Test]
    [TestOf(nameof(BatchSchedulingService.CalculateWeeklyDelay))]
    public void CalculateWeeklyDelay_SameDayAfterTargetTime_ReturnsNextWeek()
    {
        // Arrange — Thursday 23:00 UTC, target Thursday 22:00
        var utcNow = new DateTime(2026, 4, 9, 23, 0, 0, DateTimeKind.Utc); // Thursday

        // Act
        var delay = _sut.CalculateWeeklyDelay(DayOfWeek.Thursday, new TimeOnly(22, 0), utcNow);

        // Assert — 6 days + 23 hours until next Thursday 22:00
        var expected = TimeSpan.FromDays(7) - TimeSpan.FromHours(1);
        Assert.That(delay, Is.EqualTo(expected));
    }

    [Test]
    [TestOf(nameof(BatchSchedulingService.CalculateWeeklyDelay))]
    public void CalculateWeeklyDelay_AlwaysReturnsPositiveTimeSpan(
        [Values(
            DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday,
            DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday,
            DayOfWeek.Saturday)] DayOfWeek currentDay)
    {
        // Arrange — test from each day of the week at noon
        // 2026-04-05 is a Sunday
        var baseSunday = new DateTime(2026, 4, 5, 12, 0, 0, DateTimeKind.Utc);
        var daysFromSunday = ((int)currentDay - (int)DayOfWeek.Sunday + 7) % 7;
        var utcNow = baseSunday.AddDays(daysFromSunday);

        // Act
        var delay = _sut.CalculateWeeklyDelay(DayOfWeek.Thursday, new TimeOnly(22, 0), utcNow);

        // Assert
        Assert.That(delay, Is.GreaterThan(TimeSpan.Zero));
    }

    #endregion

    #region Helpers

    private IReadOnlyList<ModelConfig> CreateModels(int count)
    {
        return _fixture.CreateMany<ModelConfig>(count).ToList();
    }

    private NewsBriefRun CreateSuccessfulRun(ModelConfig model, AgentPrompt prompt)
    {
        var run = NewsBriefRun.Start(model, prompt);
        run.Complete(
            _fixture.Create<string>(),
            _fixture.Create<TimeSpan>(),
            _fixture.Create<int>(),
            _fixture.Create<int>());
        return run;
    }

    #endregion
}
