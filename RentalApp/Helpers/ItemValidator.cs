// RentalApp/Helpers/ItemValidator.cs
namespace RentalApp.Helpers;

public static class ItemValidator
{
    public static string? ValidateCreate(
        string? title,
        string? description,
        string? dailyRateString,
        int categoryId
    )
    {
        if (string.IsNullOrWhiteSpace(title))
            return "Title is required";
        if (title.Length < 5)
            return "Title must be at least 5 characters";
        if (title.Length > 100)
            return "Title must be 100 characters or fewer";
        if (description?.Length > 1000)
            return "Description must be 1000 characters or fewer";
        if (string.IsNullOrWhiteSpace(dailyRateString))
            return "Daily rate is required";
        if (!double.TryParse(dailyRateString, out var dailyRate))
            return "Daily rate must be a valid number";
        if (dailyRate <= 0)
            return "Daily rate must be greater than zero";
        if (dailyRate > 1000)
            return "Daily rate cannot exceed £1000";
        if (categoryId <= 0)
            return "A category must be selected";
        return null;
    }

    public static string? ValidateUpdate(
        string? title,
        string? description,
        string? dailyRateString
    )
    {
        if (title is not null && string.IsNullOrWhiteSpace(title))
            return "Title is required";
        if (title is not null && title.Length < 5)
            return "Title must be at least 5 characters";
        if (title is not null && title.Length > 100)
            return "Title must be 100 characters or fewer";
        if (description is not null && description.Length > 1000)
            return "Description must be 1000 characters or fewer";
        if (dailyRateString is not null)
        {
            if (dailyRateString.Trim().Length == 0)
                return "Daily rate is required";
            if (!double.TryParse(dailyRateString, out var dailyRate))
                return "Daily rate must be a valid number";
            if (dailyRate <= 0)
                return "Daily rate must be greater than zero";
            if (dailyRate > 1000)
                return "Daily rate cannot exceed £1000";
        }
        return null;
    }
}
