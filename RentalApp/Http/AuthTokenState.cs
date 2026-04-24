namespace RentalApp.Http;

/// <summary>
/// Holds the current bearer token for the authenticated session.
/// Shared as a singleton so that <see cref="ApiClient"/> and
/// <see cref="RentalApp.Services.RemoteApiService"/> always read and write the same token.
/// </summary>
/// <remarks>
/// Singleton lifetime — <see cref="RentalApp.Services.RemoteApiService"/> writes on login/logout;
/// <see cref="ApiClient"/> reads to attach the <c>Authorization: Bearer</c> header. Not thread-safe.
/// </remarks>
public class AuthTokenState
{
    /// <summary>
    /// Gets or sets the current bearer token, or <see langword="null"/> when no session is active.
    /// </summary>
    public string? CurrentToken { get; set; }
}
