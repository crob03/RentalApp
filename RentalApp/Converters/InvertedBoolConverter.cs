using System.Globalization;

namespace RentalApp.Converters;

/// <summary>
/// Converts a boolean value to its inverse: <see langword="true"/> becomes <see langword="false"/>
/// and vice versa.
/// </summary>
public class InvertedBoolConverter : IValueConverter
{
    /// <summary>
    /// Returns the logical inverse of <paramref name="value"/>.
    /// Returns <see langword="false"/> if <paramref name="value"/> is not a <see cref="bool"/>.
    /// </summary>
    /// <param name="value">The boolean value to invert.</param>
    /// <param name="targetType">The target binding type (unused).</param>
    /// <param name="parameter">An optional converter parameter (unused).</param>
    /// <param name="culture">The culture to use (unused).</param>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }

        return false;
    }

    /// <summary>
    /// Returns the logical inverse of <paramref name="value"/> for two-way bindings.
    /// Returns <see langword="false"/> if <paramref name="value"/> is not a <see cref="bool"/>.
    /// </summary>
    /// <param name="value">The boolean value to invert.</param>
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
            return !boolValue;
        }

        return false;
    }
}
