namespace FikaForecast.Domain.Enums;

/// <summary>
/// Confidence classification for a weekly summary theme based on
/// how persistently it appeared across daily briefs.
/// </summary>
public enum ConfidenceLevel
{
    High,
    Moderate,
    Dropped
}
