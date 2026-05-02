using CommunityToolkit.Mvvm.Input;
using RentalApp.Constants;
using RentalApp.Http;
using RentalApp.Services;

namespace RentalApp.ViewModels;

public abstract partial class AuthenticatedViewModel : BaseViewModel
{
    private readonly AuthTokenState _tokenState;
    private readonly ICredentialStore _credentialStore;
    private readonly INavigationService _navigationService;

    protected AuthenticatedViewModel(
        AuthTokenState tokenState,
        ICredentialStore credentialStore,
        INavigationService navigationService
    )
    {
        _tokenState = tokenState;
        _credentialStore = credentialStore;
        _navigationService = navigationService;
    }

    protected Task NavigateToAsync(string route) => _navigationService.NavigateToAsync(route);

    protected Task NavigateToAsync(string route, Dictionary<string, object> parameters) =>
        _navigationService.NavigateToAsync(route, parameters);

    protected Task NavigateBackAsync() => _navigationService.NavigateBackAsync();

    protected virtual Task<bool> ConfirmLogoutAsync() =>
        Application
            .Current?.Windows[0]
            ?.Page?.DisplayAlertAsync("Logout", "Are you sure you want to logout?", "Yes", "No")
        ?? Task.FromResult(false);

    [RelayCommand]
    private async Task LogoutAsync()
    {
        if (!await ConfirmLogoutAsync())
            return;

        await _credentialStore.ClearAsync();
        _tokenState.ClearToken();
        await _navigationService.NavigateToAsync(Routes.Login);
    }

    [RelayCommand]
    private async Task NavigateToProfileAsync() =>
        await _navigationService.NavigateToAsync(Routes.Temp);
}
