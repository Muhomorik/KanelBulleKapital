using FikaForecast.Application.Services;

namespace FikaForecast.Application.Tests.Services;

[TestFixture]
[TestOf(typeof(IsoWeek))]
public class IsoWeekTests
{
    #region Compute

    // Standard mid-year date — sanity baseline.
    [TestCase(2026, 4, 9, 2026, 15)]
    // ISO week 1 starts on the Monday of the week containing the first Thursday of the year.
    // 2026-01-01 is a Thursday → week 01 of 2026.
    [TestCase(2026, 1, 1, 2026, 1)]
    // 2025-01-01 is a Wednesday → the first Thursday is 2025-01-02, so week 01 of 2025.
    [TestCase(2025, 1, 1, 2025, 1)]
    // 2024-12-30 is a Monday of week 01-2025 (rolls forward into next ISO year).
    [TestCase(2024, 12, 30, 2025, 1)]
    // 2024-01-01 is a Monday → week 01 of 2024.
    [TestCase(2024, 1, 1, 2024, 1)]
    // 2023-01-01 is a Sunday → it's still week 52 of 2022 (rolls backward into prior ISO year).
    [TestCase(2023, 1, 1, 2022, 52)]
    // 2020 is a 53-week ISO year; 2020-12-31 is a Thursday → week 53 of 2020.
    [TestCase(2020, 12, 31, 2020, 53)]
    // Day after: 2021-01-01 is a Friday → still week 53 of 2020.
    [TestCase(2021, 1, 1, 2020, 53)]
    public void Compute_ReturnsIsoYearAndWeek(
        int year, int month, int day, int expectedYear, int expectedWeek)
    {
        // Arrange
        var instant = new DateTimeOffset(year, month, day, 12, 0, 0, TimeSpan.FromHours(2));

        // Act
        var (isoYear, isoWeek) = IsoWeek.Compute(instant);

        // Assert
        Assert.That(isoYear, Is.EqualTo(expectedYear));
        Assert.That(isoWeek, Is.EqualTo(expectedWeek));
    }

    [Test]
    public void Compute_UsesLocalCalendarDate_NotUtcDate()
    {
        // 2023-01-01 22:00 in +03:00 is 2022-12-31 19:00 UTC — different ISO years depending on basis.
        // The user sees "Jan 1 2023" locally, so the function should return the 2022-W52 the local date maps to.
        // Arrange
        var instant = new DateTimeOffset(2023, 1, 1, 22, 0, 0, TimeSpan.FromHours(3));

        // Act
        var (isoYear, isoWeek) = IsoWeek.Compute(instant);

        // Assert — Sunday 2023-01-01 local → week 52 of 2022 per ISO 8601.
        Assert.That(isoYear, Is.EqualTo(2022));
        Assert.That(isoWeek, Is.EqualTo(52));
    }

    #endregion

    #region Format

    [TestCase(2026, 4, 9, "2026-W15")]
    [TestCase(2024, 12, 30, "2025-W01")]
    [TestCase(2020, 12, 31, "2020-W53")]
    [TestCase(2023, 1, 1, "2022-W52")]
    public void Format_ReturnsZeroPaddedTag(int year, int month, int day, string expected)
    {
        // Arrange
        var instant = new DateTimeOffset(year, month, day, 12, 0, 0, TimeSpan.FromHours(1));

        // Act
        var formatted = IsoWeek.Format(instant);

        // Assert
        Assert.That(formatted, Is.EqualTo(expected));
    }

    #endregion
}
