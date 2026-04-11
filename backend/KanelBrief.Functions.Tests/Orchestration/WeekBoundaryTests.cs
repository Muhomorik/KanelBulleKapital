using KanelBrief.Functions.Orchestration;

namespace KanelBrief.Functions.Tests.Orchestration;

[TestFixture]
[TestOf(typeof(DailyPipelineOrchestrator))]
public class WeekBoundaryTests
{
    [TestCase(DayOfWeek.Monday, 0)]
    [TestCase(DayOfWeek.Tuesday, 1)]
    [TestCase(DayOfWeek.Wednesday, 2)]
    [TestCase(DayOfWeek.Thursday, 3)]
    [TestCase(DayOfWeek.Friday, 4)]
    [TestCase(DayOfWeek.Saturday, 5)]
    [TestCase(DayOfWeek.Sunday, 6)]
    public void CalculateWeekBoundaries_AnyDay_PrevMondayIsMondayOfPreviousWeek(
        DayOfWeek dayOfWeek, int expectedDaysFromMonday)
    {
        // Find an actual date with this DayOfWeek (week of April 6-12, 2026: Mon=6, Sun=12)
        var monday = new DateTime(2026, 4, 6);
        var testDate = new DateTimeOffset(monday.AddDays(expectedDaysFromMonday), TimeSpan.Zero);

        var (prevMonday, prevSunday) = DailyPipelineOrchestrator.CalculateWeekBoundaries(testDate);

        Assert.That(prevMonday.DayOfWeek, Is.EqualTo(DayOfWeek.Monday), "prevMonday should be a Monday");
        Assert.That(prevSunday.DayOfWeek, Is.EqualTo(DayOfWeek.Monday), "prevSunday boundary should be next Monday (exclusive)");
        Assert.That((prevSunday - prevMonday).Days, Is.EqualTo(7), "Should span exactly 7 days");
    }

    [Test]
    public void CalculateWeekBoundaries_MondayExecution_PreviousWeekRange()
    {
        // Monday April 6, 2026
        var monday = new DateTimeOffset(2026, 4, 6, 9, 0, 0, TimeSpan.Zero);

        var (prevMonday, prevSunday) = DailyPipelineOrchestrator.CalculateWeekBoundaries(monday);

        Assert.That(prevMonday, Is.EqualTo(new DateTime(2026, 3, 30)));
        Assert.That(prevSunday, Is.EqualTo(new DateTime(2026, 4, 6)));
    }

    [Test]
    public void CalculateWeekBoundaries_WednesdayExecution_PreviousWeekRange()
    {
        // Wednesday April 8, 2026
        var wednesday = new DateTimeOffset(2026, 4, 8, 9, 0, 0, TimeSpan.Zero);

        var (prevMonday, prevSunday) = DailyPipelineOrchestrator.CalculateWeekBoundaries(wednesday);

        Assert.That(prevMonday, Is.EqualTo(new DateTime(2026, 3, 30)));
        Assert.That(prevSunday, Is.EqualTo(new DateTime(2026, 4, 6)));
    }

    [Test]
    public void CalculateWeekBoundaries_SundayExecution_PreviousWeekRange()
    {
        // Sunday April 12, 2026 — tricky case (DayOfWeek=0)
        var sunday = new DateTimeOffset(2026, 4, 12, 9, 0, 0, TimeSpan.Zero);

        var (prevMonday, prevSunday) = DailyPipelineOrchestrator.CalculateWeekBoundaries(sunday);

        // Sunday belongs to the week Mon Apr 6 - Sun Apr 12
        // So previous week is Mon Mar 30 - Sun Apr 5
        Assert.That(prevMonday, Is.EqualTo(new DateTime(2026, 3, 30)));
        Assert.That(prevSunday, Is.EqualTo(new DateTime(2026, 4, 6)));
    }

    [Test]
    public void CalculateWeekBoundaries_CrossingYearBoundary_HandlesCorrectly()
    {
        // Monday January 5, 2026
        var monday = new DateTimeOffset(2026, 1, 5, 9, 0, 0, TimeSpan.Zero);

        var (prevMonday, prevSunday) = DailyPipelineOrchestrator.CalculateWeekBoundaries(monday);

        Assert.That(prevMonday, Is.EqualTo(new DateTime(2025, 12, 29)));
        Assert.That(prevSunday, Is.EqualTo(new DateTime(2026, 1, 5)));
    }
}
