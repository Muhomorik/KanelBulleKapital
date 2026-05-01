using KanelBrief.Core.Time;

namespace KanelBrief.Core.Tests.Time;

[TestFixture]
[TestOf(typeof(IsoWeek))]
public class IsoWeekTests
{
    [Test]
    public void Format_MidYearMonday_ReturnsCanonicalLabel()
    {
        // 2026-04-20 is the Monday starting ISO week 17.
        var instant = new DateTimeOffset(2026, 4, 20, 0, 0, 0, TimeSpan.Zero);

        var label = IsoWeek.Format(instant);

        Assert.That(label, Is.EqualTo("2026-W17"));
    }

    [Test]
    public void Format_FirstWeekOfYear_PadsWeekNumberToTwoDigits()
    {
        // 2026-01-05 is Monday of ISO week 02 (week 01 ended 2026-01-04 in this calendar).
        var instant = new DateTimeOffset(2026, 1, 5, 12, 0, 0, TimeSpan.Zero);

        var label = IsoWeek.Format(instant);

        Assert.That(label, Is.EqualTo("2026-W02"));
    }

    [Test]
    public void Compute_DateInLateDecemberThatBelongsToNextIsoYear_ReportsNextYear()
    {
        // 2025-12-29 is Monday of ISO week 01 of 2026 — boundary edge case.
        var instant = new DateTimeOffset(2025, 12, 29, 0, 0, 0, TimeSpan.Zero);

        var (year, week) = IsoWeek.Compute(instant);

        Assert.That(year, Is.EqualTo(2026));
        Assert.That(week, Is.EqualTo(1));
    }

    [Test]
    public void Compute_DateInEarlyJanuaryThatBelongsToPreviousIsoYear_ReportsPreviousYear()
    {
        // 2027-01-03 is Sunday of ISO week 53 of 2026 (2026 is a 53-week ISO year).
        var instant = new DateTimeOffset(2027, 1, 3, 0, 0, 0, TimeSpan.Zero);

        var (year, week) = IsoWeek.Compute(instant);

        Assert.That(year, Is.EqualTo(2026));
        Assert.That(week, Is.EqualTo(53));
    }

    [Test]
    public void Compute_RespectsLocalDateOfTheOffset_NotUtc()
    {
        // 2026-01-04 23:30 in +02:00 is still 2026-01-04 locally → ISO week 01.
        // (Shifted to UTC it would be 2026-01-04 21:30 — same date, but the test pins
        // the "local-date" semantics so callers in non-UTC zones don't see surprise weeks.)
        var instant = new DateTimeOffset(2026, 1, 4, 23, 30, 0, TimeSpan.FromHours(2));

        var label = IsoWeek.Format(instant);

        Assert.That(label, Is.EqualTo("2026-W01"));
    }
}
