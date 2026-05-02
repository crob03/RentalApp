using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Contracts.Requests;
using RentalApp.Helpers;
using RentalApp.Services;

namespace RentalApp.ViewModels;

public partial class RegisterViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private readonly INavigationService _navigationService;

    [ObservableProperty] private string firstName = string.Empty;
    [ObservableProperty] private string lastName = string.Empty;
    [ObservableProperty] private string email = string.Empty;
    [ObservableProperty] private string password = string.Empty;
    [ObservableProperty] private string confirmPassword = string.Empty;
    [ObservableProperty] private bool acceptTerms;

    public RegisterViewModel(IAuthService authService, INavigationService navigationService)
    {
        _authService = authService;
        _navigationService = navigationService;
        Title = "Register";
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName == nameof(IsBusy))
            RegisterCommand.NotifyCanExecuteChanged();
    }

    private bool CanRegister() => !IsBusy;

    [RelayCommand(CanExecute = nameof(CanRegister))]
    private async Task RegisterAsync()
    {
        if (!ValidateForm())
            return;

        IsBusy = true;
        ClearError();

        try
        {
            await _authService.RegisterAsync(new RegisterRequest(FirstName, LastName, Email, Password));
            await _navigationService.NavigateBackAsync();
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
    private async Task NavigateBackToLoginAsync() =>
        await _navigationService.NavigateBackAsync();

    private bool ValidateForm()
    {
        var error = RegistrationValidator.Validate(FirstName, LastName, Email, Password, ConfirmPassword, AcceptTerms);
        if (error is not null)
        {
            SetError(error);
            return false;
        }
        return true;
    }
}
