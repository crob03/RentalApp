using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Constants;
using RentalApp.Contracts.Requests;
using RentalApp.Http;
using RentalApp.Services;

namespace RentalApp.ViewModels;

/// <summary>
/// Singleton view model for the login page. Manages email/password form state, optional credential
/// auto-fill, and delegates authentication to <see cref="IAuthService"/>. Owns bearer-token state
/// and credential persistence after a successful login.
/// </summary>
public partial class LoginViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IAuthService _authService;
    private readonly AuthTokenState _tokenState;
    private readonly ICredentialStore _credentialStore;
    private readonly INavigationService _navigationService;

    /// <summary>The email address entered by the user.</summary>
    [ObservableProperty]
    private string email = string.Empty;

    /// <summary>The password entered by the user.</summary>
    [ObservableProperty]
    private string password = string.Empty;

    /// <summary>Whether the user has opted to persist credentials for auto-fill on next launch.</summary>
    [ObservableProperty]
    private bool rememberMe;

    /// <summary>
    /// Initialises the view model with authentication and navigation dependencies.
    /// </summary>
    /// <param name="authService">Used to perform login requests.</param>
    /// <param name="tokenState">Receives the bearer token on successful login.</param>
    /// <param name="credentialStore">Persisted credential store read on app start and written when <see cref="RememberMe"/> is set.</param>
    /// <param name="navigationService">Used to navigate to <see cref="Routes.Main"/> after login.</param>
    public LoginViewModel(
        IAuthService authService,
        AuthTokenState tokenState,
        ICredentialStore credentialStore,
        INavigationService navigationService
    )
    {
        _authService = authService;
        _tokenState = tokenState;
        _credentialStore = credentialStore;
        _navigationService = navigationService;
        Title = "Login";
    }

    /// <summary>
    /// Pre-fills the form with saved credentials if present, and sets <see cref="RememberMe"/> to
    /// <see langword="true"/> to signal that credentials were restored.
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
    /// Handles the <c>sessionExpired</c> query attribute set when the app detects a token expiry,
    /// showing a contextual error message.
    /// </summary>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("sessionExpired", out var value) && value is true)
            SetError("Your session has expired. Please log in again.");
        else
            ClearError();
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName == nameof(IsBusy))
            LoginCommand.NotifyCanExecuteChanged();
    }

    private bool CanLogin() => !IsBusy;

    /// <summary>Validates credentials and navigates to <see cref="Routes.Main"/> on success.</summary>
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

        try
        {
            var response = await _authService.LoginAsync(new LoginRequest(Email, Password));
            _tokenState.CurrentToken = response.Token;

            if (RememberMe)
                await _credentialStore.SaveAsync(Email, Password);

            await _navigationService.NavigateToAsync(Routes.Main);
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Navigates to the registration page.</summary>
    [RelayCommand]
    private async Task NavigateToRegisterAsync() =>
        await _navigationService.NavigateToAsync(Routes.Register);
}
