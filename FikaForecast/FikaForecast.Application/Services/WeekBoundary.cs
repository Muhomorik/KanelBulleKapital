namespace FikaForecast.Application.Services;

/// <summary>
/// Calculates the previous completed Mon→Sun week window for weekly aggregation.
/// Mirrors the backend <c>WeeklyAggregationPipeline.CalculateWeekBoundaries</c> so the
/// WPF manual run scopes the same set of daily briefs as the scheduled cloud pipeline.
/// </summary>
public static class WeekBoundary
{
    /// <summary>
    /// Returns the last completed Monday→Monday window relative to <paramref name="now"/>.
    /// <paramref name="endExclusive"/> is the Monday after the window's Sunday — i.e. use
    /// <c>ts &gt;= start &amp;&amp; ts &lt; endExclusive</c> to filter briefs into the window.
    /// </summary>
    public static (DateTimeOffset start, DateTimeOffset endExclusive) CalculateLastCompletedWeek(DateTimeOffset now)
    {
        var daysFromMonday = ((int)now.DayOfWeek + 6) % 7;
        var thisMonday = new DateTimeOffset(now.Date.AddDays(-daysFromMonday), now.Offset);
        var start = thisMonday.AddDays(-7);
        return (start, thisMonday);
    }
}
