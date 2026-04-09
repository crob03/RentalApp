namespace RentalApp.Services;

/// <summary>
/// Holds the current bearer token for the authenticated session.
/// Shared as a singleton so that <see cref="AuthRefreshHandler"/> and
/// <see cref="ApiAuthenticationService"/> always read and write the same token.
/// </summary>
public class AuthTokenState
{
    /// <summary>
    /// Gets or sets the current bearer token, or <see langword="null"/> when no session is active.
    /// </summary>
    public string? CurrentToken { get; set; }
}
