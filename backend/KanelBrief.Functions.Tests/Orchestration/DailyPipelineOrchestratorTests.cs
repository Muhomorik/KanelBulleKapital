using Cronos;
using KanelBrief.Functions.Orchestration;

namespace KanelBrief.Functions.Tests.Orchestration;

/// <summary>
/// Tests for Daily Pipeline Orchestrator cron schedules.
/// Verifies that timer triggers fire at the correct times.
/// </summary>
[TestFixture]
public class DailyPipelineOrchestratorTests
{
    [Test]
    public void DailyBriefSchedule_IsValidCronExpression()
    {
        var schedule = DailyPipelineOrchestrator.DAILY_BRIEF_SCHEDULE;

        
        // Should not throw; valid cron format
        var cron = CronExpression.Parse(schedule);
        Assert.That(cron, Is.Not.Null);
    }

    [Test]
    public void DailyBriefSchedule_RunsEveryDayAt8Utc()
    {
        var cron = CronExpression.Parse(DailyPipelineOrchestrator.DAILY_BRIEF_SCHEDULE);

        // Test: Tuesday, April 7, 2026, 7:59 AM UTC (before trigger)
        var beforeTrigger = new DateTime(2026, 4, 7, 7, 59, 0, DateTimeKind.Utc);
        var nextRun = cron.GetNextOccurrence(beforeTrigger);

        Assert.That(nextRun, Is.Not.Null, "Should find next occurrence");
        Assert.That(nextRun!.Value.Hour, Is.EqualTo(8), "Should run at 8 AM UTC");
        Assert.That(nextRun.Value.Day, Is.EqualTo(7), "Should run same day");
    }

    [Test]
    public void DailyBriefSchedule_RunsAgainNextDay()
    {
        var cron = CronExpression.Parse(DailyPipelineOrchestrator.DAILY_BRIEF_SCHEDULE);

        // After 8 AM today, next occurrence should be 8 AM tomorrow
        var after8Am = new DateTime(2026, 4, 7, 8, 1, 0, DateTimeKind.Utc);
        var nextRun = cron.GetNextOccurrence(after8Am);

        Assert.That(nextRun, Is.Not.Null);
        Assert.That(nextRun!.Value.Hour, Is.EqualTo(8));
        Assert.That(nextRun.Value.Day, Is.EqualTo(8), "Should run next day");
    }

    [Test]
    public void WeeklyAggregationSchedule_IsValidCronExpression()
    {
        var schedule = DailyPipelineOrchestrator.WEEKLY_AGGREGATION_SCHEDULE;

        // Should not throw; valid cron format
        var cron = CronExpression.Parse(schedule);
        Assert.That(cron, Is.Not.Null);
    }

    [Test]
    public void WeeklyAggregationSchedule_RunsOnMondayAt9Utc()
    {
        var cron = CronExpression.Parse(DailyPipelineOrchestrator.WEEKLY_AGGREGATION_SCHEDULE);

        // Test: Tuesday, April 7, 2026 (need to find next Monday)
        var tuesday = new DateTime(2026, 4, 7, 8, 0, 0, DateTimeKind.Utc);
        var nextRun = cron.GetNextOccurrence(tuesday);

        Assert.That(nextRun, Is.Not.Null);
        Assert.That(nextRun!.Value.DayOfWeek, Is.EqualTo(DayOfWeek.Monday), "Should run on Monday");
        Assert.That(nextRun.Value.Hour, Is.EqualTo(9), "Should run at 9 AM UTC");
    }

    [Test]
    public void WeeklyAggregationSchedule_SkipsToFollowingMonday()
    {
        var cron = CronExpression.Parse(DailyPipelineOrchestrator.WEEKLY_AGGREGATION_SCHEDULE);

        // On Monday after 9 AM, next occurrence is next Monday at 9 AM
        var mondayAfter9Am = new DateTime(2026, 4, 6, 9, 1, 0, DateTimeKind.Utc); // Monday after 9 AM
        var nextRun = cron.GetNextOccurrence(mondayAfter9Am);

        Assert.That(nextRun, Is.Not.Null);
        Assert.That(nextRun!.Value.DayOfWeek, Is.EqualTo(DayOfWeek.Monday));
        Assert.That(nextRun.Value.Day, Is.EqualTo(13), "Should jump to following Monday (April 13)");
        Assert.That(nextRun.Value.Hour, Is.EqualTo(9));
    }

    [Test]
    public void DailyBriefSchedule_ExactValue_Is_0_8_Star_Star_Star()
    {
        var schedule = DailyPipelineOrchestrator.DAILY_BRIEF_SCHEDULE;
        Assert.That(schedule, Is.EqualTo("0 8 * * *"));
    }

    [Test]
    public void WeeklyAggregationSchedule_ExactValue_Is_0_9_Star_Star_1()
    {
        var schedule = DailyPipelineOrchestrator.WEEKLY_AGGREGATION_SCHEDULE;
        Assert.That(schedule, Is.EqualTo("0 9 * * 1"));
    }
}
