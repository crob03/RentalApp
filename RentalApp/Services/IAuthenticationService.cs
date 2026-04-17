using RentalApp.Models;

namespace RentalApp.Services;

/// <summary>
/// Defines the contract for authentication operations including login, registration, and logout.
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Raised when the user's authentication status changes.
    /// The event argument is <see langword="true"/> when the user has authenticated and
    /// <see langword="false"/> when they have logged out.
    /// </summary>
    event EventHandler<bool>? AuthenticationStateChanged;

    /// <summary>
    /// Gets a value indicating whether a user is currently authenticated.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Gets the currently authenticated user, or <see langword="null"/> if no user is logged in.
    /// </summary>
    UserProfile? CurrentUser { get; }

    /// <summary>
    /// Authenticates the user with the supplied credentials.
    /// </summary>
    /// <param name="email">The user's email address.</param>
    /// <param name="password">The user's password.</param>
    /// <param name="rememberMe">
    /// When <see langword="true"/>, the credentials are persisted for automatic login on next launch.
    /// </param>
    /// <returns>An <see cref="AuthenticationResult"/> indicating success or failure.</returns>
    Task<AuthenticationResult> LoginAsync(string email, string password, bool rememberMe = false);

    /// <summary>
    /// Creates a new user account with the supplied details.
    /// </summary>
    /// <param name="firstName">The new user's first name.</param>
    /// <param name="lastName">The new user's last name.</param>
    /// <param name="email">The new user's email address.</param>
    /// <param name="password">The new user's chosen password.</param>
    /// <returns>An <see cref="AuthenticationResult"/> indicating success or failure.</returns>
    Task<AuthenticationResult> RegisterAsync(
        string firstName,
        string lastName,
        string email,
        string password
    );

    /// <summary>
    /// Logs out the current user and clears any persisted credentials.
    /// </summary>
    Task LogoutAsync();
}
