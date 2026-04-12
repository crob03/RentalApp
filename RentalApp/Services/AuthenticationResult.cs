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
    /// Creates a successful result.
    /// </summary>
    public static AuthenticationResult Success() => new AuthenticationResult { IsSuccess = true };

    /// <summary>
    /// Creates a failed result with the supplied error message.
    /// </summary>
    /// <param name="errorMessage">A human-readable description of why the operation failed.</param>
    public static AuthenticationResult Failure(string errorMessage) =>
        new AuthenticationResult { IsSuccess = false, ErrorMessage = errorMessage };
}
