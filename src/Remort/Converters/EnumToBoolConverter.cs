using System.Globalization;
using System.Windows.Data;

namespace Remort.Converters;

/// <summary>
/// Converts an enum value to a boolean (true when the value matches the parameter).
/// Used for binding radio buttons to enum properties.
/// </summary>
[ValueConversion(typeof(Enum), typeof(bool))]
public sealed class EnumToBoolConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value?.Equals(parameter) ?? false;
    }

    /// <inheritdoc/>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is true && parameter is not null ? parameter : System.Windows.Data.Binding.DoNothing;
    }
}
