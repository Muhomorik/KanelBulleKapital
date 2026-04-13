using Cronos;
using KanelBrief.Functions.Orchestration;

namespace KanelBrief.Functions.Tests.Orchestration;

/// <summary>
/// Tests for Daily Pipeline Orchestrator cron schedules.
/// Verifies that timer triggers fire at the correct times.
/// </summary>
[TestFixture]
[TestOf(typeof(DailyPipelineOrchestrator))]
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
    public void DailyBriefSchedule_FromMidnight_RunsAt00Utc()
    {
        var cron = CronExpression.Parse(DailyPipelineOrchestrator.DAILY_BRIEF_SCHEDULE);

        // Tuesday, April 7, 2026, 23:59 UTC — next fire should be the 00 UTC slot the next day.
        var beforeMidnight = new DateTime(2026, 4, 7, 23, 59, 0, DateTimeKind.Utc);
        var nextRun = cron.GetNextOccurrence(beforeMidnight);

        Assert.That(nextRun, Is.Not.Null);
        Assert.That(nextRun!.Value.Hour, Is.EqualTo(0));
        Assert.That(nextRun.Value.Minute, Is.EqualTo(0));
        Assert.That(nextRun.Value.Day, Is.EqualTo(8));
    }

    [Test]
    public void DailyBriefSchedule_RunsEveryFourHours_Produces6SlotsPerDay()
    {
        var cron = CronExpression.Parse(DailyPipelineOrchestrator.DAILY_BRIEF_SCHEDULE);

        var expectedHours = new[] { 0, 4, 8, 12, 16, 20 };
        var start = new DateTime(2026, 4, 7, 0, 0, 0, DateTimeKind.Utc).AddSeconds(-1);

        var actualHours = new List<int>();
        var cursor = start;
        for (var i = 0; i < expectedHours.Length; i++)
        {
            var next = cron.GetNextOccurrence(cursor);
            Assert.That(next, Is.Not.Null);
            actualHours.Add(next!.Value.Hour);
            cursor = next.Value;
        }

        Assert.That(actualHours, Is.EqualTo(expectedHours));
    }

    [Test]
    public void DailyBriefSchedule_After20Utc_NextRunIs00NextDay()
    {
        var cron = CronExpression.Parse(DailyPipelineOrchestrator.DAILY_BRIEF_SCHEDULE);

        var after20 = new DateTime(2026, 4, 7, 20, 1, 0, DateTimeKind.Utc);
        var nextRun = cron.GetNextOccurrence(after20);

        Assert.That(nextRun, Is.Not.Null);
        Assert.That(nextRun!.Value.Hour, Is.EqualTo(0));
        Assert.That(nextRun.Value.Day, Is.EqualTo(8), "Should roll over to next day");
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
    public void WeeklyAggregationSchedule_RunsOnThursdayAt21Utc()
    {
        var cron = CronExpression.Parse(DailyPipelineOrchestrator.WEEKLY_AGGREGATION_SCHEDULE);

        // Monday, April 6, 2026, 08:00 UTC — next Thursday is April 9.
        var monday = new DateTime(2026, 4, 6, 8, 0, 0, DateTimeKind.Utc);
        var nextRun = cron.GetNextOccurrence(monday);

        Assert.That(nextRun, Is.Not.Null);
        Assert.That(nextRun!.Value.DayOfWeek, Is.EqualTo(DayOfWeek.Thursday));
        Assert.That(nextRun.Value.Hour, Is.EqualTo(21));
        Assert.That(nextRun.Value.Minute, Is.EqualTo(0));
    }

    [Test]
    public void WeeklyAggregationSchedule_SkipsToFollowingThursday()
    {
        var cron = CronExpression.Parse(DailyPipelineOrchestrator.WEEKLY_AGGREGATION_SCHEDULE);

        // Thursday, April 9, 2026, 21:01 UTC — next fire is the following Thursday (April 16).
        var thursdayAfter21 = new DateTime(2026, 4, 9, 21, 1, 0, DateTimeKind.Utc);
        var nextRun = cron.GetNextOccurrence(thursdayAfter21);

        Assert.That(nextRun, Is.Not.Null);
        Assert.That(nextRun!.Value.DayOfWeek, Is.EqualTo(DayOfWeek.Thursday));
        Assert.That(nextRun.Value.Day, Is.EqualTo(16));
        Assert.That(nextRun.Value.Hour, Is.EqualTo(21));
    }

    [Test]
    public void DailyBriefSchedule_ExactValue_Is_0_EveryFourHours()
    {
        var schedule = DailyPipelineOrchestrator.DAILY_BRIEF_SCHEDULE;
        Assert.That(schedule, Is.EqualTo("0 */4 * * *"));
    }

    [Test]
    public void WeeklyAggregationSchedule_ExactValue_Is_0_21_Star_Star_4()
    {
        var schedule = DailyPipelineOrchestrator.WEEKLY_AGGREGATION_SCHEDULE;
        Assert.That(schedule, Is.EqualTo("0 21 * * 4"));
    }
}
