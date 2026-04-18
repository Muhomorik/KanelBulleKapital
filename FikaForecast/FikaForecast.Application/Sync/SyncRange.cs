namespace FikaForecast.Application.Sync;

/// <summary>
/// How far back to sync agent runs from the backend.
/// </summary>
public enum SyncRange
{
    OneDay,
    OneWeek
}

public static class SyncRangeExtensions
{
    /// <summary>
    /// Returns the (from, to) date range for the given sync range, anchored to UTC today.
    /// Backend PartitionKey uses <c>yyyy-MM-dd</c> in UTC.
    /// </summary>
    public static (DateOnly From, DateOnly To) ToDateRange(this SyncRange range)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        return range switch
        {
            SyncRange.OneDay => (today.AddDays(-1), today),
            SyncRange.OneWeek => (today.AddDays(-7), today),
            _ => (today.AddDays(-1), today)
        };
    }
}
