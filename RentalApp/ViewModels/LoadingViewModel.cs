using RentalApp.Constants;
using RentalApp.Contracts.Requests;
using RentalApp.Http;
using RentalApp.Services;

namespace RentalApp.ViewModels;

public class LoadingViewModel : BaseViewModel
{
    private readonly ICredentialStore _credentialStore;
    private readonly IAuthService _authService;
    private readonly AuthTokenState _tokenState;
    private readonly INavigationService _navigationService;

    public LoadingViewModel(
        ICredentialStore credentialStore,
        IAuthService authService,
        AuthTokenState tokenState,
        INavigationService navigationService
    )
    {
        _credentialStore = credentialStore;
        _authService = authService;
        _tokenState = tokenState;
        _navigationService = navigationService;
    }

    public async Task InitializeAsync()
    {
        var credentials = await _credentialStore.GetAsync();

        if (credentials is null)
        {
            await _navigationService.NavigateToAsync(Routes.Login);
            return;
        }

        try
        {
            var response = await _authService.LoginAsync(
                new LoginRequest(credentials.Value.Email, credentials.Value.Password)
            );
            _tokenState.CurrentToken = response.Token;
            await _navigationService.NavigateToAsync(Routes.Main);
        }
        catch
        {
            await _navigationService.NavigateToAsync(Routes.Login);
        }
    }
}
