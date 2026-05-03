using System.Globalization;
using System.Text.RegularExpressions;

namespace RentalApp.Converters;

/// <summary>
/// Converts a rental status string to a human-readable display string.
/// PascalCase values (e.g. "OutForRent") are split into words ("Out For Rent").
/// Already-spaced values (e.g. "Out for Rent") are returned unchanged.
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
