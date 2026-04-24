using RentalApp.Models;

namespace RentalApp.Services;

/// <summary>
/// Manages authentication state on top of <see cref="IApiService"/>.
/// Handles the current user, authentication events, and credential persistence.
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly IApiService _api;
    private readonly ICredentialStore _credentialStore;
    private User? _currentUser;

    /// <inheritdoc/>
    public event EventHandler<bool>? AuthenticationStateChanged;

    /// <inheritdoc/>
    public bool IsAuthenticated => _currentUser != null;

    /// <inheritdoc/>
    public User? CurrentUser => _currentUser;

    /// <summary>Initialises a new instance of <see cref="AuthenticationService"/>.</summary>
    /// <param name="api">Data-transport service used to authenticate and fetch user data.</param>
    /// <param name="credentialStore">Store used to persist credentials when Remember Me is enabled.</param>
    public AuthenticationService(IApiService api, ICredentialStore credentialStore)
    {
        _api = api;
        _credentialStore = credentialStore;
    }

    /// <inheritdoc/>
    public async Task<AuthenticationResult> LoginAsync(
        string email,
        string password,
        bool rememberMe = false
    )
    {
        try
        {
            await _api.LoginAsync(email, password);

            if (rememberMe)
                await _credentialStore.SaveAsync(email, password);

            _currentUser = await _api.GetCurrentUserAsync();
            AuthenticationStateChanged?.Invoke(this, true);
            return AuthenticationResult.Success();
        }
        catch (Exception ex)
        {
            return AuthenticationResult.Failure(ex.Message);
        }
    }

    /// <inheritdoc/>
    public async Task<AuthenticationResult> RegisterAsync(
        string firstName,
        string lastName,
        string email,
        string password
    )
    {
        try
        {
            await _api.RegisterAsync(firstName, lastName, email, password);
            return AuthenticationResult.Success();
        }
        catch (Exception ex)
        {
            return AuthenticationResult.Failure(ex.Message);
        }
    }

    /// <inheritdoc/>
    public async Task LogoutAsync()
    {
        _currentUser = null;
        await _api.LogoutAsync();
        await _credentialStore.ClearAsync();
        AuthenticationStateChanged?.Invoke(this, false);
    }
}
