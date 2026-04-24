using RentalApp.Models;

namespace RentalApp.Services;

/// <summary>
/// Data-transport interface for the rental API. Implementations switch between
/// the remote HTTP backend (<see cref="RemoteApiService"/>) and the local
/// database backend (<see cref="LocalApiService"/>).
/// </summary>
/// <remarks>All methods return <see cref="RentalApp.Models"/> DTOs — never EF entities.</remarks>
public interface IApiService
{
    // ── Authentication ─────────────────────────────────────────────

    /// <summary>Authenticates the user and establishes a session.</summary>
    /// <param name="email">User's email address.</param>
    /// <param name="password">User's password.</param>
    /// <exception cref="UnauthorizedAccessException">Thrown when credentials are invalid.</exception>
    Task LoginAsync(string email, string password);

    /// <summary>Registers a new user account.</summary>
    /// <param name="firstName">First name.</param>
    /// <param name="lastName">Last name.</param>
    /// <param name="email">Email address.</param>
    /// <param name="password">Password (minimum 8 characters, must include uppercase, lowercase, digit, and special character).</param>
    /// <exception cref="InvalidOperationException">Thrown when the email is already registered.</exception>
    Task RegisterAsync(string firstName, string lastName, string email, string password);

    /// <summary>Returns the full profile of the currently authenticated user.</summary>
    Task<User> GetCurrentUserAsync();

    /// <summary>Returns the public profile of the specified user.</summary>
    /// <param name="userId">Identifier of the user to retrieve.</param>
    Task<User> GetUserAsync(int userId);

    /// <summary>Ends the current session and clears any stored session state.</summary>
    Task LogoutAsync();
}
