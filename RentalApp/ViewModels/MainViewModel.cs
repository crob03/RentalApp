using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Constants;
using RentalApp.Contracts.Responses;
using RentalApp.Services;

namespace RentalApp.ViewModels;

public partial class MainViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private CurrentUserResponse? currentUser;

    [ObservableProperty]
    private string welcomeMessage = string.Empty;

    public MainViewModel(IAuthService authService, INavigationService navigationService)
    {
        _authService = authService;
        _navigationService = navigationService;
        Title = "Dashboard";
    }

    public Task InitializeAsync() => RunAsync(LoadUserAsync);

    private async Task LoadUserAsync()
    {
        CurrentUser = await _authService.GetCurrentUserAsync();
        WelcomeMessage = $"Welcome, {CurrentUser.FirstName} {CurrentUser.LastName}!";
    }

    [RelayCommand]
    private async Task NavigateToProfileAsync() =>
        await _navigationService.NavigateToAsync(Routes.Temp);

    [RelayCommand]
    private async Task NavigateToItemsListAsync() =>
        await _navigationService.NavigateToAsync(Routes.ItemsList);

    [RelayCommand]
    private async Task NavigateToNearbyItemsAsync() =>
        await _navigationService.NavigateToAsync(Routes.NearbyItems);

    [RelayCommand]
    private async Task NavigateToCreateItemAsync() =>
        await _navigationService.NavigateToAsync(Routes.CreateItem);

    [RelayCommand]
    private Task RefreshDataAsync() => RunAsync(LoadUserAsync);
}
