using System.Text.RegularExpressions;

namespace RentalApp.Helpers;

/// <summary>
/// Validates registration form inputs.
/// </summary>
/// <remarks>
/// Returns <see langword="null"/> when all inputs are valid.
/// Returns the first failing error message otherwise.
/// </remarks>
public static partial class RegistrationValidator
{
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase)]
    private static partial Regex EmailRegex();

    // Ensures password contains at least one uppercase letter,
    // one lowercase letter, one number, and one special character
    [GeneratedRegex(@"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d)(?=.*[^A-Za-z0-9]).+$")]
    private static partial Regex PasswordRegex();

    /// <summary>
    /// Validates all registration form fields.
    /// </summary>
    /// <returns>
    /// <see langword="null"/> if all fields are valid;
    /// otherwise the first validation error message.
    /// </returns>
    public static string? Validate(
        string firstName,
        string lastName,
        string email,
        string password,
        string confirmPassword,
        bool acceptTerms
    )
    {
        if (string.IsNullOrWhiteSpace(firstName))
            return "First name is required";

        if (firstName.Length > 50)
            return "First name must be 50 characters or fewer";

        if (string.IsNullOrWhiteSpace(lastName))
            return "Last name is required";

        if (lastName.Length > 50)
            return "Last name must be 50 characters or fewer";

        if (string.IsNullOrWhiteSpace(email))
            return "Email is required";

        if (!EmailRegex().IsMatch(email))
            return "Please enter a valid email address";

        if (string.IsNullOrWhiteSpace(password))
            return "Password is required";

        if (password.Length < 8)
            return "Password must be at least 8 characters long";

        if (!PasswordRegex().IsMatch(password))
            return "Password must contain an uppercase letter, lowercase letter, number, and special character";

        if (password != confirmPassword)
            return "Passwords do not match";

        if (!acceptTerms)
            return "Please accept the terms and conditions";

        return null;
    }
}
