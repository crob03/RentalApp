using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Database.Models;
using RentalApp.Services;

namespace RentalApp.ViewModels;

/// <summary>
/// View model for the main dashboard page. Loads the authenticated user's profile data
/// and provides navigation commands to other sections of the application.
/// </summary>
public partial class MainViewModel : BaseViewModel
{
    private readonly IAuthenticationService _authService;
    private readonly INavigationService _navigationService;

    /// <summary>
    /// The currently authenticated user.
    /// </summary>
    [ObservableProperty]
    private User? currentUser;

    /// <summary>
    /// Personalised welcome message derived from the current user's name.
    /// </summary>
    [ObservableProperty]
    private string welcomeMessage = string.Empty;

    /// <summary>
    /// Indicates whether the current user has administrator privileges.
    /// Used to control the visibility of admin-only UI elements.
    /// </summary>
    [ObservableProperty]
    private bool isAdmin;

    /// <summary>
    /// Initialises a new instance of <see cref="MainViewModel"/> for design-time support.
    /// </summary>
    public MainViewModel()
    {
        Title = "Dashboard";
    }

    /// <summary>
    /// Initialises a new instance of <see cref="MainViewModel"/> with the required services
    /// and immediately loads the current user's data.
    /// </summary>
    /// <param name="authService">The authentication service used to retrieve the current user.</param>
    /// <param name="navigationService">The navigation service used to transition between pages.</param>
    public MainViewModel(IAuthenticationService authService, INavigationService navigationService)
    {
        _authService = authService;
        _navigationService = navigationService;
        Title = "Dashboard";

        LoadUserData();
    }

    /// <summary>
    /// Populates <see cref="CurrentUser"/> and <see cref="WelcomeMessage"/> from the
    /// authenticated user held by <see cref="IAuthenticationService"/>.
    /// </summary>
    private void LoadUserData()
    {
        CurrentUser = _authService.CurrentUser;

        if (CurrentUser != null)
        {
            WelcomeMessage = $"Welcome, {CurrentUser.FullName}!";
        }
    }

    /// <summary>
    /// Prompts the user for confirmation and, if confirmed, logs out and navigates to the
    /// login page.
    /// </summary>
    [RelayCommand]
    private async Task LogoutAsync()
    {
        var result = await Application.Current.MainPage.DisplayAlert(
            "Logout",
            "Are you sure you want to logout?",
            "Yes",
            "No"
        );

        if (result)
        {
            await _authService.LogoutAsync();
            await _navigationService.NavigateToAsync("LoginPage");
        }
    }

    /// <summary>
    /// Navigates to the user profile page.
    /// </summary>
    [RelayCommand]
    private async Task NavigateToProfileAsync()
    {
        await _navigationService.NavigateToAsync("TempPage");
    }

    /// <summary>
    /// Navigates to the application settings page.
    /// </summary>
    [RelayCommand]
    private async Task NavigateToSettingsAsync()
    {
        await _navigationService.NavigateToAsync("TempPage");
    }

    /// <summary>
    /// Navigates to the user management page. Restricted to administrators; shows an access
    /// denied alert if the current user does not have the admin role.
    /// </summary>
    [RelayCommand]
    private async Task NavigateToUserListAsync()
    {
        if (!IsAdmin)
        {
            await Application.Current.MainPage.DisplayAlert(
                "Access Denied",
                "You don't have permission to access admin features.",
                "OK"
            );
            return;
        }

        await _navigationService.NavigateToAsync("UserListPage");
    }

    /// <summary>
    /// Reloads the current user's data from the authentication service.
    /// </summary>
    [RelayCommand]
    private async Task RefreshDataAsync()
    {
        try
        {
            IsBusy = true;
            LoadUserData();

            // Simulate refresh delay
            await Task.Delay(1000);
        }
        catch (Exception ex)
        {
            SetError($"Failed to refresh data: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
