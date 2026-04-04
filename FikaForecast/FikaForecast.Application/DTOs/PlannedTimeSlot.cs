namespace FikaForecast.Application.DTOs;

/// <summary>
/// Represents a planned execution time in the daily batch schedule (e.g. 08:00, 12:00).
/// </summary>
/// <param name="PlannedTime">The scheduled execution time.</param>
/// <param name="IsPast">True if the slot time has already passed when the schedule was built.</param>
public sealed record PlannedTimeSlot(TimeOnly PlannedTime, bool IsPast);
