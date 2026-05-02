// RentalApp/Helpers/ItemValidator.cs
namespace RentalApp.Helpers;

/// <summary>
/// Validates item create and update form inputs.
/// </summary>
/// <remarks>
/// Returns <see langword="null"/> when all inputs are valid.
/// Returns the first failing error message otherwise.
/// </remarks>
public static class ItemValidator
{
    /// <summary>
    /// Validates all required fields for a new item listing.
    /// </summary>
    /// <returns>
    /// <see langword="null"/> if all fields are valid;
    /// otherwise the first validation error message.
    /// </returns>
    public static string? ValidateCreate(
        string? title,
        string? description,
        string? dailyRateString,
        int categoryId
    )
    {
        var titleError = ValidateTitle(title);
        if (titleError is not null)
            return titleError;
        if (description?.Length > 1000)
            return "Description must be 1000 characters or fewer";
        if (string.IsNullOrWhiteSpace(dailyRateString))
            return "Daily rate is required";
        var rateError = ValidateRate(dailyRateString);
        if (rateError is not null)
            return rateError;
        if (categoryId <= 0)
            return "A category must be selected";
        return null;
    }

    /// <summary>
    /// Validates the fields supplied for an item update; only non-<see langword="null"/> fields are validated.
    /// </summary>
    /// <returns>
    /// <see langword="null"/> if all supplied fields are valid;
    /// otherwise the first validation error message.
    /// </returns>
    public static string? ValidateUpdate(
        string? title,
        string? description,
        string? dailyRateString
    )
    {
        if (title is not null)
        {
            var titleError = ValidateTitle(title);
            if (titleError is not null)
                return titleError;
        }
        if (description is not null && description.Length > 1000)
            return "Description must be 1000 characters or fewer";
        if (dailyRateString is not null)
        {
            var rateError = ValidateRate(dailyRateString);
            if (rateError is not null)
                return rateError;
        }
        return null;
    }

    private static string? ValidateTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return "Title is required";
        if (title.Length < 5)
            return "Title must be at least 5 characters";
        if (title.Length > 100)
            return "Title must be 100 characters or fewer";
        return null;
    }

    private static string? ValidateRate(string dailyRateString)
    {
        if (dailyRateString.Trim().Length == 0)
            return "Daily rate is required";
        if (!double.TryParse(dailyRateString, out var dailyRate))
            return "Daily rate must be a valid number";
        if (dailyRate <= 0)
            return "Daily rate must be greater than zero";
        if (dailyRate > 1000)
            return "Daily rate cannot exceed £1000";
        return null;
    }
}
