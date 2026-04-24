using System.Globalization;

namespace RentalApp.Converters;

/// <summary>
/// Converts a string to a boolean value.
/// Returns <see langword="true"/> if the string is non-null and non-whitespace;
/// otherwise <see langword="false"/>.
/// </summary>
public class StringToBoolConverter : IValueConverter
{
    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="value"/> is a non-null, non-whitespace
    /// string; otherwise <see langword="false"/>.
    /// </summary>
    /// <param name="value">The string value to evaluate.</param>
    /// <param name="targetType">The target binding type (unused).</param>
    /// <param name="parameter">An optional converter parameter (unused).</param>
    /// <param name="culture">The culture to use (unused).</param>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string stringValue)
        {
            return !string.IsNullOrWhiteSpace(stringValue);
        }

        return false;
    }

    /// <summary>
    /// Converts a boolean back to a string for two-way bindings.
    /// Returns <c>"true"</c> when <paramref name="value"/> is <see langword="true"/>;
    /// otherwise an empty string.
    /// </summary>
    /// <param name="value">The boolean value to convert.</param>
    /// <param name="targetType">The target binding type (unused).</param>
    /// <param name="parameter">An optional converter parameter (unused).</param>
    /// <param name="culture">The culture to use (unused).</param>
    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    )
    {
        if (value is bool boolValue)
        {
            return boolValue ? "true" : string.Empty;
        }

        return string.Empty;
    }
}
