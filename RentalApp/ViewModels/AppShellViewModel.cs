using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Constants;
using RentalApp.Services;

namespace RentalApp.ViewModels
{
    /// <summary>
    /// View model for the application shell, responsible for managing top-level navigation
    /// commands and reacting to authentication state changes.
    /// </summary>
    public partial class AppShellViewModel : BaseViewModel
    {
        private readonly IAuthenticationService _authService;
        private readonly INavigationService _navigationService;

        /// <summary>
        /// Initialises a new instance of <see cref="AppShellViewModel"/> for design-time support.
        /// </summary>
        public AppShellViewModel()
        {
            Title = "RentalApp";
        }

        /// <summary>
        /// Initialises a new instance of <see cref="AppShellViewModel"/> with the required services.
        /// Subscribes to <see cref="IAuthenticationService.AuthenticationStateChanged"/> so that
        /// command availability is refreshed whenever the user's authentication status changes.
        /// </summary>
        /// <param name="authService">The authentication service used to check and update auth state.</param>
        /// <param name="navigationService">The navigation service used to perform page transitions.</param>
        public AppShellViewModel(
            IAuthenticationService authService,
            INavigationService navigationService
        )
        {
            _authService = authService;
            _navigationService = navigationService;
            _authService.AuthenticationStateChanged += OnAuthenticationStateChanged;
            Title = "RentalApp";
        }

        /// <summary>
        /// Guard used by commands that require an authenticated user.
        /// </summary>
        /// <returns><see langword="true"/> if a user is currently authenticated; otherwise <see langword="false"/>.</returns>
        private bool CanExecuteAuthenticatedAction()
        {
            return _authService.IsAuthenticated;
        }

        /// <summary>
        /// Handles the <see cref="IAuthenticationService.AuthenticationStateChanged"/> event by
        /// notifying all guarded commands to re-evaluate their can-execute state.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="isAuthenticated">
        /// <see langword="true"/> if the user has just authenticated; <see langword="false"/> if they
        /// have logged out.
        /// </param>
        private void OnAuthenticationStateChanged(object? sender, bool isAuthenticated)
        {
            LogoutCommand.NotifyCanExecuteChanged();
            NavigateToProfileCommand.NotifyCanExecuteChanged();
            NavigateToSettingsCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// Navigates to the current user's profile page.
        /// </summary>
        [RelayCommand]
        private async Task NavigateToProfileAsync()
        {
            await _navigationService.NavigateToAsync(Routes.Temp);
        }

        /// <summary>
        /// Navigates to the application settings page.
        /// </summary>
        [RelayCommand]
        private async Task NavigateToSettingsAsync()
        {
            await _navigationService.NavigateToAsync(Routes.Temp);
        }

        /// <summary>
        /// Logs out the current user and navigates back to the login page.
        /// This command can only execute when a user is authenticated.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanExecuteAuthenticatedAction))]
        private async Task LogoutAsync()
        {
            await _authService.LogoutAsync();
            await _navigationService.NavigateToAsync(Routes.LoginPage);

            LogoutCommand.NotifyCanExecuteChanged();
            NavigateToProfileCommand.NotifyCanExecuteChanged();
            NavigateToSettingsCommand.NotifyCanExecuteChanged();
        }
    }
}
