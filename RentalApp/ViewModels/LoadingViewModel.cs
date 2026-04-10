using RentalApp.Services;

namespace RentalApp.ViewModels;

/// <summary>
/// View model for the loading/splash page. On initialisation it attempts to auto-login using
/// stored credentials. If successful the user is taken directly to the main page; if not, they
/// are sent to the login page.
/// </summary>
public class LoadingViewModel : BaseViewModel
{
    private readonly ICredentialStore _credentialStore;
    private readonly IAuthenticationService _authService;
    private readonly INavigationService _navigationService;

    /// <summary>
    /// Initialises a new instance of <see cref="LoadingViewModel"/> with the required services.
    /// </summary>
    /// <param name="credentialStore">The credential store used to retrieve persisted login credentials.</param>
    /// <param name="authService">The authentication service used to attempt auto-login.</param>
    /// <param name="navigationService">The navigation service used to transition to the appropriate page.</param>
    public LoadingViewModel(
        ICredentialStore credentialStore,
        IAuthenticationService authService,
        INavigationService navigationService
    )
    {
        _credentialStore = credentialStore;
        _authService = authService;
        _navigationService = navigationService;
    }

    /// <summary>
    /// Attempts to log in automatically using persisted credentials.
    /// Navigates to <c>MainPage</c> on success, or to the root login route on failure.
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
            await _navigationService.NavigateToAsync("MainPage");
        else
            await _navigationService.NavigateToRootAsync();
    }
}
