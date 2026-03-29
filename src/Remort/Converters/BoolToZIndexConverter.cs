using System.Globalization;
using System.Windows.Data;

namespace Remort.Converters;

/// <summary>
/// Converts a boolean to a Panel.ZIndex value: <see langword="true"/> → 10 (on top), <see langword="false"/> → -1 (behind).
/// </summary>
[ValueConversion(typeof(bool), typeof(int))]
public sealed class BoolToZIndexConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is true ? 10 : -1;
    }

    /// <inheritdoc/>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
