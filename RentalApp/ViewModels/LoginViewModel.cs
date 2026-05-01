using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Constants;
using RentalApp.Services;

namespace RentalApp.ViewModels;

/// <summary>
/// View model for the login page. Manages the login form fields, input validation,
/// and delegates authentication to <see cref="IAuthenticationService"/>.
/// </summary>
public partial class LoginViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IAuthenticationService _authService;
    private readonly INavigationService _navigationService;
    private readonly ICredentialStore _credentialStore;

    /// <summary>
    /// The email address entered by the user.
    /// </summary>
    [ObservableProperty]
    private string email = string.Empty;

    /// <summary>
    /// The password entered by the user.
    /// </summary>
    [ObservableProperty]
    private string password = string.Empty;

    /// <summary>
    /// Whether the user has opted to persist their credentials for automatic login on next launch.
    /// </summary>
    [ObservableProperty]
    private bool rememberMe;

    /// <summary>
    /// Initialises a new instance of <see cref="LoginViewModel"/> with the required services.
    /// </summary>
    /// <param name="authService">The authentication service used to perform login.</param>
    /// <param name="navigationService">The navigation service used to transition between pages.</param>
    /// <param name="credentialStore">The credential store used to restore saved credentials on page load.</param>
    public LoginViewModel(
        IAuthenticationService authService,
        INavigationService navigationService,
        ICredentialStore credentialStore
    )
    {
        _authService = authService;
        _navigationService = navigationService;
        _credentialStore = credentialStore;
        Title = "Login";
    }

    /// <summary>
    /// Populates the email, password, and remember-me fields from the credential store, if saved
    /// credentials exist. Called each time the login page appears.
    /// </summary>
    public async Task InitializeAsync()
    {
        var credentials = await _credentialStore.GetAsync();
        if (credentials is null)
            return;

        Email = credentials.Value.Email;
        Password = credentials.Value.Password;
        RememberMe = true;
    }

    /// <summary>
    /// Receives navigation query parameters. Sets a session-expired error when redirected here
    /// by <see cref="RentalApp.Http.ApiClient"/> after a token refresh failure; clears any stale error otherwise.
    /// </summary>
    /// <param name="query">The query parameters passed by the navigation system.</param>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("sessionExpired", out var value) && value is true)
            SetError("Your session has expired. Please log in again.");
        else
            ClearError();
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName == nameof(IsBusy))
            LoginCommand.NotifyCanExecuteChanged();
    }

    private bool CanLogin() => !IsBusy;

    /// <summary>
    /// Validates the login form and attempts to authenticate the user.
    /// Navigates to the main page on success, or surfaces an error message on failure.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            SetError("Please enter both email and password");
            return;
        }

        IsBusy = true;
        ClearError();

        var result = await _authService.LoginAsync(Email, Password, RememberMe);

        if (result.IsSuccess)
        {
            await _navigationService.NavigateToAsync(Routes.Main);
        }
        else
        {
            SetError(result.ErrorMessage);
        }

        IsBusy = false;
    }

    /// <summary>
    /// Navigates to the registration page.
    /// </summary>
    [RelayCommand]
    private async Task NavigateToRegisterAsync()
    {
        await _navigationService.NavigateToAsync(Routes.Register);
    }
}
