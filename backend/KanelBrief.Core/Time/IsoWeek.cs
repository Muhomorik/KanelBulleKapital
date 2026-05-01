using System.Globalization;

namespace KanelBrief.Core.Time;

/// <summary>
/// ISO 8601 week number + ISO-week year. Mirrors the FikaForecast helper so
/// that period labels stamped on backend runs match what the WPF app and
/// frontend display.
/// </summary>
public static class IsoWeek
{
    /// <summary>
    /// Returns the ISO-week year and ISO week number (1..53) for the given instant's date.
    /// Evaluated in the instant's own offset — the calendar date the user perceives.
    /// </summary>
    public static (int Year, int Week) Compute(DateTimeOffset instant)
    {
        var date = instant.DateTime.Date;
        var week = ISOWeek.GetWeekOfYear(date);
        var year = ISOWeek.GetYear(date);
        return (year, week);
    }

    /// <summary>
    /// Formats as the canonical <c>YYYY-Www</c> tag used in wire metadata and filenames.
    /// </summary>
    public static string Format(DateTimeOffset instant)
    {
        var (year, week) = Compute(instant);
        return string.Create(CultureInfo.InvariantCulture, $"{year:0000}-W{week:00}");
    }
}
