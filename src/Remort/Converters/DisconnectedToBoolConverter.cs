using System.Globalization;
using System.Windows.Data;
using Remort.Connection;

namespace Remort.Converters;

/// <summary>
/// Converts <see cref="ConnectionState.Disconnected"/> to <see langword="true"/>;
/// all other states to <see langword="false"/>.
/// </summary>
[ValueConversion(typeof(ConnectionState), typeof(bool))]
public sealed class DisconnectedToBoolConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is ConnectionState state
            && state == ConnectionState.Disconnected;
    }

    /// <inheritdoc/>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
