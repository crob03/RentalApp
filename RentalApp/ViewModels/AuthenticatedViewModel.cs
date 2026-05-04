using CommunityToolkit.Mvvm.Input;
using RentalApp.Constants;
using RentalApp.Services.Auth;
using RentalApp.Services.Navigation;

namespace RentalApp.ViewModels;

/// <summary>
/// Abstract base class for all post-authentication view models.
/// Provides <see cref="LogoutCommand"/>, <see cref="NavigateToProfileCommand"/>, and protected navigation
/// helpers, so subclasses do not need to hold their own <see cref="INavigationService"/> field.
/// </summary>
public abstract partial class AuthenticatedViewModel : BaseViewModel
{
    private readonly AuthTokenState _tokenState;
    private readonly ICredentialStore _credentialStore;
    private readonly INavigationService _navigationService;

    /// <summary>
    /// Initialises the view model with the required authentication and navigation dependencies.
    /// </summary>
    /// <param name="tokenState">Singleton bearer-token holder cleared on logout.</param>
    /// <param name="credentialStore">Persisted credential store cleared on logout.</param>
    /// <param name="navigationService">Navigation service used by the protected helpers.</param>
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

    /// <summary>Navigates to the given <paramref name="route"/>.</summary>
    protected Task NavigateToAsync(string route) => _navigationService.NavigateToAsync(route);

    /// <summary>Navigates to the given <paramref name="route"/> with the supplied query <paramref name="parameters"/>.</summary>
    protected Task NavigateToAsync(string route, Dictionary<string, object> parameters) =>
        _navigationService.NavigateToAsync(route, parameters);

    /// <summary>Navigates back in the shell navigation stack.</summary>
    protected Task NavigateBackAsync() => _navigationService.NavigateBackAsync();

    /// <summary>
    /// Prompts the user to confirm logout. Returns <see langword="true"/> if the user confirms.
    /// Override in tests or derived classes to suppress the alert.
    /// </summary>
    protected virtual Task<bool> ConfirmLogoutAsync() =>
        Application
            .Current?.Windows[0]
            ?.Page?.DisplayAlertAsync("Logout", "Are you sure you want to logout?", "Yes", "No")
        ?? Task.FromResult(false);

    /// <summary>
    /// Confirms logout with the user, then clears persisted credentials, revokes the bearer token,
    /// and navigates to the login route.
    /// </summary>
    [RelayCommand]
    private async Task LogoutAsync()
    {
        if (!await ConfirmLogoutAsync())
            return;

        await _credentialStore.ClearAsync();
        _tokenState.ClearToken();
        await _navigationService.NavigateToAsync(Routes.Login);
    }

    /// <summary>Navigates to the user-profile page.</summary>
    [RelayCommand]
    private async Task NavigateToProfileAsync() =>
        await _navigationService.NavigateToAsync(Routes.UserProfile);
}
