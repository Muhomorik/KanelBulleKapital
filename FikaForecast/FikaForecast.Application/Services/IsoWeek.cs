using System.Globalization;

namespace FikaForecast.Application.Services;

/// <summary>
/// ISO 8601 week number + ISO-week year. Matches the frontend's <c>isoWeekNumber</c>
/// algorithm so exported filenames line up with what the user sees in the UI.
/// </summary>
public static class IsoWeek
{
    /// <summary>
    /// Returns the ISO-week year and ISO week number (1..53) for the given instant's date.
    /// Evaluated in the instant's own offset — the calendar date the user perceives.
    /// </summary>
    public static (int Year, int Week) Compute(DateTimeOffset instant)
    {
        // .NET's ISOWeek works on DateTime; use the offset's date to match
        // what the user sees locally rather than shifting to UTC.
        var date = instant.DateTime.Date;
        var week = ISOWeek.GetWeekOfYear(date);
        var year = ISOWeek.GetYear(date);
        return (year, week);
    }

    /// <summary>
    /// Formats as the canonical <c>YYYY-Www</c> tag used in filenames and metadata.
    /// </summary>
    public static string Format(DateTimeOffset instant)
    {
        var (year, week) = Compute(instant);
        return string.Create(CultureInfo.InvariantCulture, $"{year:0000}-W{week:00}");
    }
}
