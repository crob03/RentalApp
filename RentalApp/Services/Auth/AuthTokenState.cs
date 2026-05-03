namespace RentalApp.Services.Auth;

/// <summary>
/// Holds the current bearer token for the authenticated session.
/// Shared as a singleton so that <see cref="Http.ApiClient"/> and ViewModels always read and write the same token.
/// </summary>
/// <remarks>
/// Singleton lifetime — ViewModels write on login/logout;
/// <see cref="Http.ApiClient"/> reads to attach the <c>Authorization: Bearer</c> header. Not thread-safe.
/// </remarks>
public class AuthTokenState
{
    private string? _token;

    /// <summary>
    /// Raised when the authentication state changes (token set or cleared).
    /// The event argument is <see langword="true"/> when authenticated, <see langword="false"/> when cleared.
    /// </summary>
    public event EventHandler<bool>? AuthenticationStateChanged;

    /// <summary>
    /// Gets or sets the current bearer token, or <see langword="null"/> when no session is active.
    /// Setting this property raises the <see cref="AuthenticationStateChanged"/> event.
    /// </summary>
    public string? CurrentToken
    {
        get => _token;
        set
        {
            _token = value;
            AuthenticationStateChanged?.Invoke(this, value is not null);
        }
    }

    /// <summary>
    /// Gets a value indicating whether an authenticated session is currently active.
    /// </summary>
    public bool HasSession => _token is not null;

    /// <summary>
    /// Clears the current token by setting <see cref="CurrentToken"/> to <see langword="null"/>.
    /// Raises the <see cref="AuthenticationStateChanged"/> event with <see langword="false"/>.
    /// </summary>
    public void ClearToken() => CurrentToken = null;
}
