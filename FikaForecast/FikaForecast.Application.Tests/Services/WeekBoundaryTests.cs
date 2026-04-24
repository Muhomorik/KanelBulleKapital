using FikaForecast.Application.Services;

namespace FikaForecast.Application.Tests.Services;

[TestFixture]
[TestOf(typeof(WeekBoundary))]
public class WeekBoundaryTests
{
    [TestCase(DayOfWeek.Monday, 0)]
    [TestCase(DayOfWeek.Tuesday, 1)]
    [TestCase(DayOfWeek.Wednesday, 2)]
    [TestCase(DayOfWeek.Thursday, 3)]
    [TestCase(DayOfWeek.Friday, 4)]
    [TestCase(DayOfWeek.Saturday, 5)]
    [TestCase(DayOfWeek.Sunday, 6)]
    public void CalculateLastCompletedWeek_AnyDay_ReturnsPreviousMondayToMondaySpan(
        DayOfWeek dayOfWeek, int daysFromMonday)
    {
        // Arrange — week of April 6-12, 2026 (Mon=6, Sun=12).
        var monday = new DateTime(2026, 4, 6);
        var testDate = new DateTimeOffset(monday.AddDays(daysFromMonday), TimeSpan.Zero);

        // Act
        var (start, endExclusive) = WeekBoundary.CalculateLastCompletedWeek(testDate);

        // Assert
        Assert.That(start.DayOfWeek, Is.EqualTo(DayOfWeek.Monday), "start should be a Monday");
        Assert.That(endExclusive.DayOfWeek, Is.EqualTo(DayOfWeek.Monday), "endExclusive should be the next Monday");
        Assert.That((endExclusive - start).TotalDays, Is.EqualTo(7), "window should span exactly 7 days");
    }

    [Test]
    public void CalculateLastCompletedWeek_MondayExecution_ReturnsPriorWeek()
    {
        // Arrange
        var monday = new DateTimeOffset(2026, 4, 6, 9, 0, 0, TimeSpan.Zero);

        // Act
        var (start, endExclusive) = WeekBoundary.CalculateLastCompletedWeek(monday);

        // Assert
        Assert.That(start.Date, Is.EqualTo(new DateTime(2026, 3, 30)));
        Assert.That(endExclusive.Date, Is.EqualTo(new DateTime(2026, 4, 6)));
    }

    [Test]
    public void CalculateLastCompletedWeek_FridayExecution_ReturnsMonToSun()
    {
        // Arrange — matches the user's real scenario: today is Fri 2026-04-24.
        var friday = new DateTimeOffset(2026, 4, 24, 14, 30, 0, TimeSpan.Zero);

        // Act
        var (start, endExclusive) = WeekBoundary.CalculateLastCompletedWeek(friday);

        // Assert — should be Mon Apr 13 → exclusive Mon Apr 20 (i.e. covers Apr 13-19).
        Assert.That(start.Date, Is.EqualTo(new DateTime(2026, 4, 13)));
        Assert.That(endExclusive.Date, Is.EqualTo(new DateTime(2026, 4, 20)));
    }

    [Test]
    public void CalculateLastCompletedWeek_SundayExecution_HandlesDayOfWeekZero()
    {
        // Arrange — Sunday (DayOfWeek=0) is the tricky case where naive math fails.
        var sunday = new DateTimeOffset(2026, 4, 12, 9, 0, 0, TimeSpan.Zero);

        // Act
        var (start, endExclusive) = WeekBoundary.CalculateLastCompletedWeek(sunday);

        // Assert
        Assert.That(start.Date, Is.EqualTo(new DateTime(2026, 3, 30)));
        Assert.That(endExclusive.Date, Is.EqualTo(new DateTime(2026, 4, 6)));
    }

    [Test]
    public void CalculateLastCompletedWeek_CrossingYearBoundary_HandlesCorrectly()
    {
        // Arrange
        var monday = new DateTimeOffset(2026, 1, 5, 9, 0, 0, TimeSpan.Zero);

        // Act
        var (start, endExclusive) = WeekBoundary.CalculateLastCompletedWeek(monday);

        // Assert
        Assert.That(start.Date, Is.EqualTo(new DateTime(2025, 12, 29)));
        Assert.That(endExclusive.Date, Is.EqualTo(new DateTime(2026, 1, 5)));
    }

    [Test]
    public void CalculateLastCompletedWeek_ReturnsOffsetAtMidnight()
    {
        // Arrange — late-evening input should still produce a midnight-aligned window.
        var lateNight = new DateTimeOffset(2026, 4, 24, 23, 45, 0, TimeSpan.Zero);

        // Act
        var (start, endExclusive) = WeekBoundary.CalculateLastCompletedWeek(lateNight);

        // Assert
        Assert.That(start.TimeOfDay, Is.EqualTo(TimeSpan.Zero));
        Assert.That(endExclusive.TimeOfDay, Is.EqualTo(TimeSpan.Zero));
    }
}
