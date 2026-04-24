using Microsoft.Extensions.Logging;
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
    private readonly ILogger<AuthenticationService> _logger;
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
    /// <param name="logger">Logger for authentication lifecycle events.</param>
    public AuthenticationService(
        IApiService api,
        ICredentialStore credentialStore,
        ILogger<AuthenticationService> logger
    )
    {
        _api = api;
        _credentialStore = credentialStore;
        _logger = logger;
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
            _logger.LogInformation("User {UserId} logged in", _currentUser.Id);
            return AuthenticationResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Login failed: {Message}", ex.Message);
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
            _logger.LogInformation("Registration succeeded");
            return AuthenticationResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Registration failed: {Message}", ex.Message);
            return AuthenticationResult.Failure(ex.Message);
        }
    }

    /// <inheritdoc/>
    public async Task LogoutAsync()
    {
        var userId = _currentUser?.Id;
        _currentUser = null;
        await _api.LogoutAsync();
        await _credentialStore.ClearAsync();
        AuthenticationStateChanged?.Invoke(this, false);
        _logger.LogInformation("User {UserId} logged out", userId);
    }
}
