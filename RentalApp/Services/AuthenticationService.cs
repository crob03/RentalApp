using RentalApp.Models;

namespace RentalApp.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly IApiService _api;
    private readonly ICredentialStore _credentialStore;
    private User? _currentUser;

    public event EventHandler<bool>? AuthenticationStateChanged;
    public bool IsAuthenticated => _currentUser != null;
    public User? CurrentUser => _currentUser;

    public AuthenticationService(IApiService api, ICredentialStore credentialStore)
    {
        _api = api;
        _credentialStore = credentialStore;
    }

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

    public async Task LogoutAsync()
    {
        _currentUser = null;
        await _api.LogoutAsync();
        await _credentialStore.ClearAsync();
        AuthenticationStateChanged?.Invoke(this, false);
    }
}
