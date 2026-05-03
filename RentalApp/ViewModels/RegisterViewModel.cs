using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Contracts.Requests;
using RentalApp.Helpers;
using RentalApp.Services;

namespace RentalApp.ViewModels;

/// <summary>
/// Singleton view model for the registration page. Manages the registration form, delegates
/// validation to <see cref="RegistrationValidator"/>, and calls <see cref="IAuthService.RegisterAsync"/>.
/// </summary>
public partial class RegisterViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private readonly INavigationService _navigationService;

    /// <summary>The user's first name.</summary>
    [ObservableProperty]
    private string firstName = string.Empty;

    /// <summary>The user's last name.</summary>
    [ObservableProperty]
    private string lastName = string.Empty;

    /// <summary>The user's email address.</summary>
    [ObservableProperty]
    private string email = string.Empty;

    /// <summary>The chosen password.</summary>
    [ObservableProperty]
    private string password = string.Empty;

    /// <summary>Password confirmation field; must match <see cref="Password"/> to pass validation.</summary>
    [ObservableProperty]
    private string confirmPassword = string.Empty;

    /// <summary>Whether the user has accepted the terms and conditions.</summary>
    [ObservableProperty]
    private bool acceptTerms;

    /// <summary>
    /// Initialises the view model with authentication and navigation dependencies.
    /// </summary>
    /// <param name="authService">Used to submit the registration request.</param>
    /// <param name="navigationService">Used to navigate back to the login page after registration.</param>
    public RegisterViewModel(IAuthService authService, INavigationService navigationService)
    {
        _authService = authService;
        _navigationService = navigationService;
        Title = "Register";
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName == nameof(IsBusy))
            RegisterCommand.NotifyCanExecuteChanged();
    }

    private bool CanRegister() => !IsBusy;

    /// <summary>Validates the form and submits a registration request, then navigates back to login.</summary>
    [RelayCommand(CanExecute = nameof(CanRegister))]
    private async Task RegisterAsync()
    {
        if (!ValidateForm())
            return;

        IsBusy = true;
        ClearError();

        try
        {
            await _authService.RegisterAsync(
                new RegisterRequest(FirstName, LastName, Email, Password)
            );
            await _navigationService.NavigateBackAsync();
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

    /// <summary>Navigates back to the login page without submitting.</summary>
    [RelayCommand]
    private async Task NavigateBackToLoginAsync() => await _navigationService.NavigateBackAsync();

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
