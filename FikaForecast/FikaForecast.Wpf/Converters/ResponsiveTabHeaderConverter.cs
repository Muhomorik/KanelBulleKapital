using System.Globalization;
using System.Windows.Data;

namespace FikaForecast.Wpf.Converters;

/// <summary>
/// Converts window width and header content to responsive tab header text.
/// Rules:
/// - Under 1180px: show icon only
/// - 1180-1280px: show icon + text (except Batch tab shows icon only)
/// - Over 1280px: show icon + text
/// </summary>
public class ResponsiveTabHeaderConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2) return string.Empty;

        if (!double.TryParse(values[0]?.ToString(), out var width))
            return values[1]?.ToString() ?? string.Empty;

        var headerText = values[1]?.ToString() ?? string.Empty;
        var isBatchTab = values.Length > 2 && bool.TryParse(values[2]?.ToString(), out var isBatch) && isBatch;

        // Extract icon (first part before double space or first character)
        var parts = headerText.Split("  ");
        var icon = parts[0];
        var text = parts.Length > 1 ? parts[1] : string.Empty;

        // Under 1180px: icon only
        if (width < 1180)
            return icon;

        // 1180-1280px: Batch shows icon only, others show both
        if (width < 1280)
            return isBatchTab ? icon : $"{icon}  {text}";

        // Over 1280px: show both
        return $"{icon}  {text}";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
