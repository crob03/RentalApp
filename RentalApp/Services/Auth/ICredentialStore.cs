namespace RentalApp.Services.Auth;

/// <summary>
/// Defines the contract for persisting and retrieving user login credentials.
/// </summary>
public interface ICredentialStore
{
    /// <summary>
    /// Persists the supplied credentials for future auto-login.
    /// </summary>
    /// <param name="email">The user's email address.</param>
    /// <param name="password">The user's password.</param>
    Task SaveAsync(string email, string password);

    /// <summary>
    /// Retrieves the persisted credentials, or <see langword="null"/> if none are stored.
    /// </summary>
    /// <returns>
    /// A tuple of <c>(Email, Password)</c> if credentials exist; otherwise <see langword="null"/>.
    /// </returns>
    Task<(string Email, string Password)?> GetAsync();

    /// <summary>
    /// Removes any persisted credentials.
    /// </summary>
    Task ClearAsync();
}
