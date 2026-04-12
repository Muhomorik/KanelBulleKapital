using KanelBrief.Core.Pipelines;

namespace KanelBrief.Core.Tests.Pipelines;

[TestFixture]
[TestOf(typeof(WeeklyAggregationPipeline))]
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
        // Arrange
        // Find an actual date with this DayOfWeek (week of April 6-12, 2026: Mon=6, Sun=12)
        var monday = new DateTime(2026, 4, 6);
        var testDate = new DateTimeOffset(monday.AddDays(expectedDaysFromMonday), TimeSpan.Zero);

        // Act
        var (prevMonday, prevSunday) = WeeklyAggregationPipeline.CalculateWeekBoundaries(testDate);

        // Assert
        Assert.That(prevMonday.DayOfWeek, Is.EqualTo(DayOfWeek.Monday), "prevMonday should be a Monday");
        Assert.That(prevSunday.DayOfWeek, Is.EqualTo(DayOfWeek.Monday), "prevSunday boundary should be next Monday (exclusive)");
        Assert.That((prevSunday - prevMonday).Days, Is.EqualTo(7), "Should span exactly 7 days");
    }

    [Test]
    public void CalculateWeekBoundaries_MondayExecution_PreviousWeekRange()
    {
        // Arrange
        var monday = new DateTimeOffset(2026, 4, 6, 9, 0, 0, TimeSpan.Zero);

        // Act
        var (prevMonday, prevSunday) = WeeklyAggregationPipeline.CalculateWeekBoundaries(monday);

        // Assert
        Assert.That(prevMonday, Is.EqualTo(new DateTime(2026, 3, 30)));
        Assert.That(prevSunday, Is.EqualTo(new DateTime(2026, 4, 6)));
    }

    [Test]
    public void CalculateWeekBoundaries_WednesdayExecution_PreviousWeekRange()
    {
        // Arrange
        var wednesday = new DateTimeOffset(2026, 4, 8, 9, 0, 0, TimeSpan.Zero);

        // Act
        var (prevMonday, prevSunday) = WeeklyAggregationPipeline.CalculateWeekBoundaries(wednesday);

        // Assert
        Assert.That(prevMonday, Is.EqualTo(new DateTime(2026, 3, 30)));
        Assert.That(prevSunday, Is.EqualTo(new DateTime(2026, 4, 6)));
    }

    [Test]
    public void CalculateWeekBoundaries_SundayExecution_PreviousWeekRange()
    {
        // Arrange
        // Sunday April 12, 2026 — tricky case (DayOfWeek=0)
        var sunday = new DateTimeOffset(2026, 4, 12, 9, 0, 0, TimeSpan.Zero);

        // Act
        var (prevMonday, prevSunday) = WeeklyAggregationPipeline.CalculateWeekBoundaries(sunday);

        // Assert
        Assert.That(prevMonday, Is.EqualTo(new DateTime(2026, 3, 30)));
        Assert.That(prevSunday, Is.EqualTo(new DateTime(2026, 4, 6)));
    }

    [Test]
    public void CalculateWeekBoundaries_CrossingYearBoundary_HandlesCorrectly()
    {
        // Arrange
        var monday = new DateTimeOffset(2026, 1, 5, 9, 0, 0, TimeSpan.Zero);

        // Act
        var (prevMonday, prevSunday) = WeeklyAggregationPipeline.CalculateWeekBoundaries(monday);

        // Assert
        Assert.That(prevMonday, Is.EqualTo(new DateTime(2025, 12, 29)));
        Assert.That(prevSunday, Is.EqualTo(new DateTime(2026, 1, 5)));
    }
}
