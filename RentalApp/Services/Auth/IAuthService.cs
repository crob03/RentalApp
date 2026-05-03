using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;

namespace RentalApp.Services.Auth;

/// <summary>
/// Defines the contract for user authentication and profile retrieval.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Authenticates a user with their email and password.
    /// </summary>
    /// <param name="request">The user's email address and plain-text password.</param>
    /// <returns>A bearer token and its expiry together with the authenticated user's identifier.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when the credentials are invalid.</exception>
    Task<LoginResponse> LoginAsync(LoginRequest request);

    /// <summary>
    /// Creates a new user account.
    /// </summary>
    /// <param name="request">Registration details: first name, last name, email, and plain-text password.</param>
    /// <returns>The newly created user's identifier, email, and name.</returns>
    /// <exception cref="InvalidOperationException">Thrown when a user with the given email already exists.</exception>
    Task<RegisterResponse> RegisterAsync(RegisterRequest request);

    /// <summary>
    /// Returns the profile of the currently authenticated user, including item and rental statistics.
    /// </summary>
    /// <returns>Full profile of the authenticated user.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no user is currently authenticated.</exception>
    Task<CurrentUserResponse> GetCurrentUserAsync();

    /// <summary>
    /// Returns the public profile of any user by their identifier.
    /// </summary>
    /// <param name="userId">The target user's unique identifier.</param>
    /// <returns>The user's public profile including listed items, completed rentals, and reviews received.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no user with the given <paramref name="userId"/> exists.</exception>
    Task<UserProfileResponse> GetUserProfileAsync(int userId);
}
