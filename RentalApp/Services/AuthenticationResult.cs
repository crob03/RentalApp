using RentalApp.Database.Models;

namespace RentalApp.Services;

/// <summary>
/// Represents the outcome of an authentication operation such as login or registration.
/// </summary>
public class AuthenticationResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Gets or sets the error message when <see cref="IsSuccess"/> is <see langword="false"/>.
    /// Empty when the operation succeeded.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the authenticated user when <see cref="IsSuccess"/> is <see langword="true"/>,
    /// or <see langword="null"/> when the operation failed or no user object was returned.
    /// </summary>
    public User? User { get; set; }

    /// <summary>
    /// Creates a successful result containing the authenticated <paramref name="user"/>.
    /// </summary>
    /// <param name="user">The user that was authenticated.</param>
    public static AuthenticationResult Success(User user) =>
        new AuthenticationResult { IsSuccess = true, User = user };

    /// <summary>
    /// Creates a successful result without a user object, for operations such as registration
    /// where the user entity is not returned.
    /// </summary>
    public static AuthenticationResult Success() => new AuthenticationResult { IsSuccess = true };

    /// <summary>
    /// Creates a failed result with the supplied error message.
    /// </summary>
    /// <param name="errorMessage">A human-readable description of why the operation failed.</param>
    public static AuthenticationResult Failure(string errorMessage) =>
        new AuthenticationResult { IsSuccess = false, ErrorMessage = errorMessage };
}
