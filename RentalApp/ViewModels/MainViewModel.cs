using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Constants;
using RentalApp.Contracts.Responses;
using RentalApp.Http;
using RentalApp.Services;

namespace RentalApp.ViewModels;

public partial class MainViewModel : AuthenticatedViewModel
{
    private readonly IAuthService _authService;

    [ObservableProperty]
    private CurrentUserResponse? currentUser;

    [ObservableProperty]
    private string welcomeMessage = string.Empty;

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

    public async Task InitializeAsync()
    {
        CurrentUser = await _authService.GetCurrentUserAsync();
        WelcomeMessage = $"Welcome, {CurrentUser.FirstName} {CurrentUser.LastName}!";
    }

    [RelayCommand]
    private Task NavigateToItemsListAsync() => NavigateToAsync(Routes.ItemsList);

    [RelayCommand]
    private Task NavigateToNearbyItemsAsync() => NavigateToAsync(Routes.NearbyItems);

    [RelayCommand]
    private Task NavigateToCreateItemAsync() => NavigateToAsync(Routes.CreateItem);

    [RelayCommand]
    private Task RefreshDataAsync() => RunAsync(InitializeAsync);
}
