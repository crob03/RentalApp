using CommunityToolkit.Mvvm.Input;
using RentalApp.Constants;
using RentalApp.Http;
using RentalApp.Services;

namespace RentalApp.ViewModels;

public partial class AppShellViewModel : BaseViewModel
{
    private readonly AuthTokenState _tokenState;
    private readonly ICredentialStore _credentialStore;
    private readonly INavigationService _navigationService;

    public AppShellViewModel(
        AuthTokenState tokenState,
        ICredentialStore credentialStore,
        INavigationService navigationService)
    {
        _tokenState = tokenState;
        _credentialStore = credentialStore;
        _navigationService = navigationService;
        _tokenState.AuthenticationStateChanged += OnAuthenticationStateChanged;
        Title = "RentalApp";
    }

    private bool CanExecuteAuthenticatedAction() => _tokenState.HasSession;

    private void OnAuthenticationStateChanged(object? sender, bool isAuthenticated)
    {
        LogoutCommand.NotifyCanExecuteChanged();
        NavigateToProfileCommand.NotifyCanExecuteChanged();
        NavigateToSettingsCommand.NotifyCanExecuteChanged();
    }

    protected virtual Task<bool> ConfirmLogoutAsync() =>
        Application.Current?.Windows[0]?.Page?.DisplayAlertAsync(
            "Logout", "Are you sure you want to logout?", "Yes", "No"
        ) ?? Task.FromResult(false);

    [RelayCommand(CanExecute = nameof(CanExecuteAuthenticatedAction))]
    private async Task LogoutAsync()
    {
        if (!await ConfirmLogoutAsync())
            return;

        await _credentialStore.ClearAsync();
        _tokenState.ClearToken();
        await _navigationService.NavigateToAsync(Routes.Login);

        LogoutCommand.NotifyCanExecuteChanged();
        NavigateToProfileCommand.NotifyCanExecuteChanged();
        NavigateToSettingsCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private async Task NavigateToProfileAsync() =>
        await _navigationService.NavigateToAsync(Routes.Temp);

    [RelayCommand]
    private async Task NavigateToSettingsAsync() =>
        await _navigationService.NavigateToAsync(Routes.Temp);
}
