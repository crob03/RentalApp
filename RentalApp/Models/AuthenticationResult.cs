namespace RentalApp.Models;

/// <summary>
/// Represents the outcome of an authentication operation such as login or registration.
/// </summary>
public sealed record AuthenticationResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Gets the error message when <see cref="IsSuccess"/> is <see langword="false"/>.
    /// Empty when the operation succeeded.
    /// </summary>
    public string ErrorMessage { get; init; } = string.Empty;

    private AuthenticationResult() { }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static AuthenticationResult Success() => new() { IsSuccess = true };

    /// <summary>
    /// Creates a failed result with the supplied error message.
    /// </summary>
    /// <param name="errorMessage">A human-readable description of why the operation failed.</param>
    public static AuthenticationResult Failure(string errorMessage) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage };
}
