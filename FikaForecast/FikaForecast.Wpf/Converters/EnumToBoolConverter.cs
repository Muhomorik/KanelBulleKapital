using System.Globalization;
using System.Windows.Data;

namespace FikaForecast.Wpf.Converters;

/// <summary>
/// Converts an enum value to <c>true</c> when it matches the <c>ConverterParameter</c> string.
/// Used for binding <see cref="System.Windows.Controls.RadioButton.IsChecked"/> to an enum property.
/// </summary>
public class EnumToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value?.ToString() == parameter?.ToString();

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is true && parameter is string name)
            return Enum.Parse(targetType, name);
        return Binding.DoNothing;
    }
}
