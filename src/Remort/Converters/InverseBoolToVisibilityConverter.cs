using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Remort.Converters;

/// <summary>
/// Converts <see langword="true"/> to <see cref="Visibility.Collapsed"/> and <see langword="false"/> to <see cref="Visibility.Visible"/>.
/// </summary>
[ValueConversion(typeof(bool), typeof(Visibility))]
public sealed class InverseBoolToVisibilityConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is true ? Visibility.Collapsed : Visibility.Visible;
    }

    /// <inheritdoc/>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
