using RentalApp.Services;

namespace RentalApp.ViewModels;

public class LoadingViewModel : BaseViewModel
{
    private readonly ICredentialStore _credentialStore;
    private readonly IAuthenticationService _authService;
    private readonly INavigationService _navigationService;
    private readonly LoginViewModel _loginViewModel;

    public LoadingViewModel(
        ICredentialStore credentialStore,
        IAuthenticationService authService,
        INavigationService navigationService,
        LoginViewModel loginViewModel
    )
    {
        _credentialStore = credentialStore;
        _authService = authService;
        _navigationService = navigationService;
        _loginViewModel = loginViewModel;
    }

    public async Task InitializeAsync()
    {
        var credentials = await _credentialStore.GetAsync();

        if (credentials is null)
        {
            await _navigationService.NavigateToRootAsync();
            return;
        }

        var result = await _authService.LoginAsync(
            credentials.Value.Email,
            credentials.Value.Password
        );

        if (result.IsSuccess)
        {
            await _navigationService.NavigateToAsync("MainPage");
        }
        else
        {
            _loginViewModel.Email = credentials.Value.Email;
            _loginViewModel.Password = credentials.Value.Password;
            _loginViewModel.RememberMe = true;
            await _navigationService.NavigateToRootAsync();
        }
    }
}
