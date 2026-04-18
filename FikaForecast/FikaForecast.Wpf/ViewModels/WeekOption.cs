using System.Globalization;

namespace FikaForecast.Wpf.ViewModels;

/// <summary>
/// A selectable week in the manual-export dropdown.
/// Label mirrors the frontend: same-year dates are shown short
/// ("Apr 7 – Apr 13, 2026 · Week 15"); year crossings show both years.
/// </summary>
public sealed record WeekOption(DateTimeOffset WeekStart, DateTimeOffset WeekEnd)
{
    public int IsoYear => FikaForecast.Application.Services.IsoWeek.Compute(WeekStart).Year;
    public int IsoWeekNumber => FikaForecast.Application.Services.IsoWeek.Compute(WeekStart).Week;

    public string DisplayLabel
    {
        get
        {
            var ci = CultureInfo.CurrentCulture;
            string range;
            if (WeekStart.Year == WeekEnd.Year)
            {
                range = $"{WeekStart.ToString("MMM d", ci)} – {WeekEnd.ToString("MMM d, yyyy", ci)}";
            }
            else
            {
                range = $"{WeekStart.ToString("MMM d, yyyy", ci)} – {WeekEnd.ToString("MMM d, yyyy", ci)}";
            }
            return $"{range} · Week {IsoWeekNumber}";
        }
    }
}
