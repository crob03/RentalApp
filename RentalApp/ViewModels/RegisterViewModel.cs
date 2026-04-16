using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Helpers;
using RentalApp.Services;

namespace RentalApp.ViewModels;

/// <summary>
/// View model for the registration page. Manages the registration form fields, validates all
/// inputs, and delegates account creation to <see cref="IAuthenticationService"/>.
/// </summary>
public partial class RegisterViewModel : BaseViewModel
{
    private readonly IAuthenticationService _authService;
    private readonly INavigationService _navigationService;

    /// <summary>
    /// The user's first name.
    /// </summary>
    [ObservableProperty]
    private string firstName = string.Empty;

    /// <summary>
    /// The user's last name.
    /// </summary>
    [ObservableProperty]
    private string lastName = string.Empty;

    /// <summary>
    /// The user's email address.
    /// </summary>
    [ObservableProperty]
    private string email = string.Empty;

    /// <summary>
    /// The password chosen by the user.
    /// </summary>
    [ObservableProperty]
    private string password = string.Empty;

    /// <summary>
    /// Confirmation of the password; must match <see cref="Password"/> for validation to pass.
    /// </summary>
    [ObservableProperty]
    private string confirmPassword = string.Empty;

    /// <summary>
    /// Whether the user has accepted the terms and conditions.
    /// </summary>
    [ObservableProperty]
    private bool acceptTerms;

    /// <summary>
    /// Initialises a new instance of <see cref="RegisterViewModel"/> for design-time support.
    /// </summary>
    public RegisterViewModel()
    {
        Title = "Register";
    }

    /// <summary>
    /// Initialises a new instance of <see cref="RegisterViewModel"/> with the required services.
    /// </summary>
    /// <param name="authService">The authentication service used to create the new account.</param>
    /// <param name="navigationService">The navigation service used to transition between pages.</param>
    public RegisterViewModel(
        IAuthenticationService authService,
        INavigationService navigationService
    )
    {
        _authService = authService;
        _navigationService = navigationService;
        Title = "Register";
    }

    /// <summary>
    /// Validates the registration form and creates a new user account.
    /// Navigates back to the login page on success, or surfaces an error message on failure.
    /// </summary>
    [RelayCommand]
    private async Task RegisterAsync()
    {
        if (IsBusy)
            return;

        if (!ValidateForm())
            return;

        IsBusy = true;
        ClearError();

        var result = await _authService.RegisterAsync(FirstName, LastName, Email, Password);

        if (result.IsSuccess)
        {
            await _navigationService.NavigateBackAsync();
        }
        else
        {
            SetError(result.ErrorMessage);
        }

        IsBusy = false;
    }

    /// <summary>
    /// Navigates back to the login page without registering.
    /// </summary>
    [RelayCommand]
    private async Task NavigateBackToLoginAsync()
    {
        await _navigationService.NavigateBackAsync();
    }

    private bool ValidateForm()
    {
        var error = RegistrationValidator.Validate(
            FirstName,
            LastName,
            Email,
            Password,
            ConfirmPassword,
            AcceptTerms
        );

        if (error is not null)
        {
            SetError(error);
            return false;
        }

        return true;
    }
}
