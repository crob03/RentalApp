using System.Globalization;
using System.Text.RegularExpressions;

namespace RentalApp.Converters;

/// <summary>
/// Converts a PascalCase rental status string (e.g. "OutForRent") to a human-readable
/// display string with spaces (e.g. "Out For Rent").
/// </summary>
public partial class RentalStatusConverter : IValueConverter
{
    [GeneratedRegex("(?<=[a-z])(?=[A-Z])")]
    private static partial Regex PascalCaseSplitter();

    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string status)
            return PascalCaseSplitter().Replace(status, " ");
        return value;
    }

    /// <inheritdoc/>
    public object? ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => throw new NotSupportedException();
}
