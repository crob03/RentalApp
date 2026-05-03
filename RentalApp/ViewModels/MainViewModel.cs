using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Constants;
using RentalApp.Contracts.Responses;
using RentalApp.Http;
using RentalApp.Services.Auth;
using RentalApp.Services.Navigation;

namespace RentalApp.ViewModels;

/// <summary>
/// Transient view model for the main dashboard page. Fetches the current user on appearance
/// and exposes navigation commands for the primary app sections.
/// </summary>
public partial class MainViewModel : AuthenticatedViewModel
{
    private readonly IAuthService _authService;

    /// <summary>The currently authenticated user's profile data.</summary>
    [ObservableProperty]
    private CurrentUserResponse? currentUser;

    /// <summary>Personalised welcome message displayed on the dashboard.</summary>
    [ObservableProperty]
    private string welcomeMessage = string.Empty;

    /// <summary>
    /// Initialises the view model with authentication and navigation dependencies.
    /// </summary>
    /// <param name="authService">Used to fetch the current user profile.</param>
    /// <param name="navigationService">Passed to <see cref="AuthenticatedViewModel"/>.</param>
    /// <param name="tokenState">Passed to <see cref="AuthenticatedViewModel"/>.</param>
    /// <param name="credentialStore">Passed to <see cref="AuthenticatedViewModel"/>.</param>
    public MainViewModel(
        IAuthService authService,
        INavigationService navigationService,
        AuthTokenState tokenState,
        ICredentialStore credentialStore
    )
        : base(tokenState, credentialStore, navigationService)
    {
        _authService = authService;
        Title = "Dashboard";
    }

    /// <summary>
    /// Fetches the current user profile and composes the welcome message.
    /// Called from <c>MainPage.OnAppearing</c>. Errors are surfaced via <see cref="BaseViewModel.SetError"/>.
    /// </summary>
    public Task InitializeAsync() => RunAsync(LoadUserAsync);

    private async Task LoadUserAsync()
    {
        CurrentUser = await _authService.GetCurrentUserAsync();
        WelcomeMessage = $"Welcome, {CurrentUser.FirstName} {CurrentUser.LastName}!";
    }

    /// <summary>Navigates to the browseable items list.</summary>
    [RelayCommand]
    private Task NavigateToItemsListAsync() => NavigateToAsync(Routes.ItemsList);

    /// <summary>Navigates to the nearby items map/list.</summary>
    [RelayCommand]
    private Task NavigateToNearbyItemsAsync() => NavigateToAsync(Routes.NearbyItems);

    /// <summary>Navigates to the create-item page.</summary>
    [RelayCommand]
    private Task NavigateToCreateItemAsync() => NavigateToAsync(Routes.CreateItem);

    /// <summary>Re-fetches user profile data, surfacing errors via <see cref="BaseViewModel.SetError"/>.</summary>
    [RelayCommand]
    private Task RefreshDataAsync() => RunAsync(LoadUserAsync);
}
