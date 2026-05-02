using RentalApp.Contracts.Responses;

namespace RentalApp.Services;

/// <summary>
/// Wraps <see cref="IApiService"/> with authentication domain logic, managing token state,
/// credential persistence, and session lifecycle.
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Raised when the authenticated state changes; the argument is <see langword="true"/> on
    /// login and <see langword="false"/> on logout.
    /// </summary>
    event EventHandler<bool>? AuthenticationStateChanged;

    /// <summary>
    /// Gets a value indicating whether a user is currently authenticated.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Gets the profile of the currently authenticated user, or <see langword="null"/> if no
    /// session is active.
    /// </summary>
    CurrentUserResponse? CurrentUser { get; }

    /// <summary>
    /// Authenticates the user with the given credentials and optionally persists them for
    /// future auto-login.
    /// </summary>
    /// <param name="email">The user's email address.</param>
    /// <param name="password">The user's password.</param>
    /// <param name="rememberMe">
    /// When <see langword="true"/>, credentials are saved to <see cref="ICredentialStore"/>.
    /// </param>
    /// <returns>
    /// An <see cref="AuthenticationResult"/> indicating success or carrying an error message.
    /// </returns>
    Task<AuthenticationResult> LoginAsync(string email, string password, bool rememberMe = false);

    /// <summary>
    /// Registers a new user account. Does not automatically log the user in.
    /// </summary>
    /// <param name="firstName">The user's first name.</param>
    /// <param name="lastName">The user's last name.</param>
    /// <param name="email">The user's email address.</param>
    /// <param name="password">The desired password.</param>
    /// <returns>
    /// An <see cref="AuthenticationResult"/> indicating success or carrying an error message.
    /// </returns>
    Task<AuthenticationResult> RegisterAsync(
        string firstName,
        string lastName,
        string email,
        string password
    );

    /// <summary>
    /// Clears the current session, removes persisted credentials, and raises
    /// <see cref="AuthenticationStateChanged"/> with <see langword="false"/>.
    /// </summary>
    Task LogoutAsync();
}
