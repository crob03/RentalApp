using RentalApp.Services;

namespace RentalApp.ViewModels;

/// <summary>
/// View model for the loading/splash page. On initialisation it attempts to auto-login using
/// stored credentials. If successful the user is taken directly to the main page; if not, they
/// are sent to the login page with their saved email and password pre-populated.
/// </summary>
public class LoadingViewModel : BaseViewModel
{
    private readonly ICredentialStore _credentialStore;
    private readonly IAuthenticationService _authService;
    private readonly INavigationService _navigationService;
    private readonly LoginViewModel _loginViewModel;

    /// <summary>
    /// Initialises a new instance of <see cref="LoadingViewModel"/> with the required services.
    /// </summary>
    /// <param name="credentialStore">The credential store used to retrieve persisted login credentials.</param>
    /// <param name="authService">The authentication service used to attempt auto-login.</param>
    /// <param name="navigationService">The navigation service used to transition to the appropriate page.</param>
    /// <param name="loginViewModel">
    /// The singleton login view model, pre-populated with saved credentials when auto-login fails.
    /// </param>
    public LoadingViewModel(
        ICredentialStore credentialStore,
        IAuthenticationService authService,
        INavigationService navigationService,
        LoginViewModel loginViewModel
    )
    {
        _credentialStore = credentialStore;
        _authService = authService;
        _navigationService = navigationService;
        _loginViewModel = loginViewModel;
    }

    /// <summary>
    /// Attempts to log in automatically using persisted credentials.
    /// Navigates to <c>MainPage</c> on success, or to the root login route on failure,
    /// pre-filling the login form with the saved credentials and enabling the remember-me toggle.
    /// If no credentials are stored, navigates directly to the root login route.
    /// </summary>
    public async Task InitializeAsync()
    {
        var credentials = await _credentialStore.GetAsync();

        if (credentials is null)
        {
            await _navigationService.NavigateToRootAsync();
            return;
        }

        var result = await _authService.LoginAsync(
            credentials.Value.Email,
            credentials.Value.Password
        );

        if (result.IsSuccess)
        {
            await _navigationService.NavigateToAsync("MainPage");
        }
        else
        {
            _loginViewModel.Email = credentials.Value.Email;
            _loginViewModel.Password = credentials.Value.Password;
            _loginViewModel.RememberMe = true;
            await _navigationService.NavigateToRootAsync();
        }
    }
}
