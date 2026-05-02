using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Constants;
using RentalApp.Contracts.Requests;
using RentalApp.Http;
using RentalApp.Services;

namespace RentalApp.ViewModels;

public partial class LoginViewModel : BaseViewModel, IQueryAttributable
{
    private readonly IAuthService _authService;
    private readonly AuthTokenState _tokenState;
    private readonly ICredentialStore _credentialStore;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private string email = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private bool rememberMe;

    public LoginViewModel(
        IAuthService authService,
        AuthTokenState tokenState,
        ICredentialStore credentialStore,
        INavigationService navigationService)
    {
        _authService = authService;
        _tokenState = tokenState;
        _credentialStore = credentialStore;
        _navigationService = navigationService;
        Title = "Login";
    }

    public async Task InitializeAsync()
    {
        var credentials = await _credentialStore.GetAsync();
        if (credentials is null)
            return;

        Email = credentials.Value.Email;
        Password = credentials.Value.Password;
        RememberMe = true;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("sessionExpired", out var value) && value is true)
            SetError("Your session has expired. Please log in again.");
        else
            ClearError();
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName == nameof(IsBusy))
            LoginCommand.NotifyCanExecuteChanged();
    }

    private bool CanLogin() => !IsBusy;

    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            SetError("Please enter both email and password");
            return;
        }

        IsBusy = true;
        ClearError();

        try
        {
            var response = await _authService.LoginAsync(new LoginRequest(Email, Password));
            _tokenState.CurrentToken = response.Token;

            if (RememberMe)
                await _credentialStore.SaveAsync(Email, Password);

            await _navigationService.NavigateToAsync(Routes.Main);
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task NavigateToRegisterAsync() =>
        await _navigationService.NavigateToAsync(Routes.Register);
}
