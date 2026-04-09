using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Services;

namespace RentalApp.ViewModels;

/// <summary>
/// View model for the login page. Manages the login form fields, input validation,
/// and delegates authentication to <see cref="IAuthenticationService"/>.
/// </summary>
public partial class LoginViewModel : BaseViewModel
{
    private readonly IAuthenticationService _authService;
    private readonly INavigationService _navigationService;

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
    /// Initialises a new instance of <see cref="LoginViewModel"/> for design-time support.
    /// </summary>
    public LoginViewModel()
    {
        Title = "Login";
    }

    /// <summary>
    /// Initialises a new instance of <see cref="LoginViewModel"/> with the required services.
    /// </summary>
    /// <param name="authService">The authentication service used to perform login.</param>
    /// <param name="navigationService">The navigation service used to transition between pages.</param>
    public LoginViewModel(IAuthenticationService authService, INavigationService navigationService)
    {
        _authService = authService;
        _navigationService = navigationService;
        Title = "Login";
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
        if (IsBusy)
            return;

        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            SetError("Please enter both email and password");
            return;
        }

        try
        {
            IsBusy = true;
            ClearError();

            var result = await _authService.LoginAsync(Email, Password, RememberMe);

            if (result.IsSuccess)
            {
                await _navigationService.NavigateToAsync("MainPage");
            }
            else
            {
                SetError(result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            SetError($"Login failed: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Navigates to the registration page.
    /// </summary>
    [RelayCommand]
    private async Task NavigateToRegisterAsync()
    {
        await _navigationService.NavigateToAsync("RegisterPage");
    }
}
