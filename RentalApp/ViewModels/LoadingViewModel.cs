using RentalApp.Constants;
using RentalApp.Contracts.Requests;
using RentalApp.Http;
using RentalApp.Services.Auth;
using RentalApp.Services.Navigation;

namespace RentalApp.ViewModels;

/// <summary>
/// View model for the transient loading/splash page shown at app start. Checks for persisted
/// credentials and silently attempts an auto-login, navigating to <see cref="Routes.Main"/> on
/// success or <see cref="Routes.Login"/> on failure.
/// </summary>
public class LoadingViewModel : BaseViewModel
{
    private readonly ICredentialStore _credentialStore;
    private readonly IAuthService _authService;
    private readonly AuthTokenState _tokenState;
    private readonly INavigationService _navigationService;

    /// <summary>
    /// Initialises the view model with the startup dependencies.
    /// </summary>
    /// <param name="credentialStore">Read to check for persisted credentials.</param>
    /// <param name="authService">Used for the silent auto-login attempt.</param>
    /// <param name="tokenState">Receives the bearer token if auto-login succeeds.</param>
    /// <param name="navigationService">Used to navigate to the appropriate root route.</param>
    public LoadingViewModel(
        ICredentialStore credentialStore,
        IAuthService authService,
        AuthTokenState tokenState,
        INavigationService navigationService
    )
    {
        _credentialStore = credentialStore;
        _authService = authService;
        _tokenState = tokenState;
        _navigationService = navigationService;
    }

    /// <summary>
    /// Checks for persisted credentials and attempts a silent login. Navigates to
    /// <see cref="Routes.Main"/> on success or <see cref="Routes.Login"/> if no credentials
    /// are stored or the auto-login fails.
    /// </summary>
    public async Task InitializeAsync()
    {
        var credentials = await _credentialStore.GetAsync();

        if (credentials is null)
        {
            await _navigationService.NavigateToAsync(Routes.Login);
            return;
        }

        try
        {
            var response = await _authService.LoginAsync(
                new LoginRequest(credentials.Value.Email, credentials.Value.Password)
            );
            _tokenState.CurrentToken = response.Token;
            await _navigationService.NavigateToAsync(Routes.Main);
        }
        catch
        {
            await _navigationService.NavigateToAsync(Routes.Login);
        }
    }
}
