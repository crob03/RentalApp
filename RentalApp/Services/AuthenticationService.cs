using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;
using RentalApp.Http;

namespace RentalApp.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly IApiService _api;
    private readonly ICredentialStore _credentialStore;
    private readonly AuthTokenState _tokenState;
    private CurrentUserResponse? _currentUser;

    public event EventHandler<bool>? AuthenticationStateChanged;
    public bool IsAuthenticated => _currentUser != null;
    public CurrentUserResponse? CurrentUser => _currentUser;

    public AuthenticationService(
        IApiService api,
        ICredentialStore credentialStore,
        AuthTokenState tokenState
    )
    {
        _api = api;
        _credentialStore = credentialStore;
        _tokenState = tokenState;
    }

    public async Task<AuthenticationResult> LoginAsync(
        string email,
        string password,
        bool rememberMe = false
    )
    {
        try
        {
            var response = await _api.LoginAsync(new LoginRequest(email, password));
            _tokenState.CurrentToken = response.Token;

            _currentUser = await _api.GetCurrentUserAsync();

            if (rememberMe)
                await _credentialStore.SaveAsync(email, password);

            AuthenticationStateChanged?.Invoke(this, true);
            return AuthenticationResult.Success();
        }
        catch (Exception ex)
        {
            _tokenState.CurrentToken = null;
            await _credentialStore.ClearAsync();
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
            await _api.RegisterAsync(new RegisterRequest(firstName, lastName, email, password));
            return AuthenticationResult.Success();
        }
        catch (Exception ex)
        {
            return AuthenticationResult.Failure(ex.Message);
        }
    }

    public async Task LogoutAsync()
    {
        await _credentialStore.ClearAsync();
        _tokenState.CurrentToken = null;
        _currentUser = null;
        AuthenticationStateChanged?.Invoke(this, false);
    }
}
