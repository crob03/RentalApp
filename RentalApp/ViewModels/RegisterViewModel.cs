using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

        try
        {
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
        }
        catch (Exception ex)
        {
            SetError($"Registration failed: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Navigates back to the login page without registering.
    /// </summary>
    [RelayCommand]
    private async Task NavigateBackToLoginAsync()
    {
        await _navigationService.NavigateBackAsync();
    }

    /// <summary>
    /// Validates all registration form fields and sets an appropriate error message if any
    /// check fails.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if all fields are valid and registration may proceed;
    /// otherwise <see langword="false"/>.
    /// </returns>
    private bool ValidateForm()
    {
        if (string.IsNullOrWhiteSpace(FirstName))
        {
            SetError("First name is required");
            return false;
        }

        if (FirstName.Length > 50)
        {
            SetError("First name must be 50 characters or fewer");
            return false;
        }

        if (string.IsNullOrWhiteSpace(LastName))
        {
            SetError("Last name is required");
            return false;
        }

        if (LastName.Length > 50)
        {
            SetError("Last name must be 50 characters or fewer");
            return false;
        }

        if (string.IsNullOrWhiteSpace(Email))
        {
            SetError("Email is required");
            return false;
        }

        if (!IsValidEmail(Email))
        {
            SetError("Please enter a valid email address");
            return false;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            SetError("Password is required");
            return false;
        }

        if (Password.Length < 8)
        {
            SetError("Password must be at least 8 characters long");
            return false;
        }

        if (!IsValidPassword(Password))
        {
            SetError(
                "Password must contain an uppercase letter, lowercase letter, number, and special character"
            );
            return false;
        }

        if (Password != ConfirmPassword)
        {
            SetError("Passwords do not match");
            return false;
        }

        if (!AcceptTerms)
        {
            SetError("Please accept the terms and conditions");
            return false;
        }

        return true;
    }

    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    // Ensures password contains at least one uppercase letter,
    // one lowercase letter, one number, and one special character
    private static readonly Regex PasswordRegex = new(
        @"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d)(?=.*[^A-Za-z0-9]).+$",
        RegexOptions.Compiled
    );

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="email"/> matches a basic
    /// <c>local@domain.tld</c> format.
    /// </summary>
    /// <param name="email">The email address to validate.</param>
    private static bool IsValidEmail(string email) => EmailRegex.IsMatch(email);

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="password"/> contains at least one
    /// uppercase letter, one lowercase letter, one digit, and one special character.
    /// </summary>
    /// <param name="password">The password to validate.</param>
    private static bool IsValidPassword(string password) => PasswordRegex.IsMatch(password);
}
