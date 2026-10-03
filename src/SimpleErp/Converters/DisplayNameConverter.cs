using System.Globalization;
using System.Windows.Data;
using SimpleErp.Core.Services;

namespace SimpleErp.Converters;

/// <summary>Shows enum values from the core (positions, statuses, …) by their human-readable names.</summary>
public sealed class DisplayNameConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => DisplayNames.For(value);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
