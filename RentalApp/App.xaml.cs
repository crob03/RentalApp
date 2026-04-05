using RentalApp.Services;
using RentalApp.ViewModels;

namespace RentalApp;

public partial class App : Application
{
    private readonly IServiceProvider _serviceProvider;

    public App(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        InitializeComponent();

        Routing.RegisterRoute(nameof(Views.MainPage), typeof(Views.MainPage));
        Routing.RegisterRoute(nameof(Views.LoginPage), typeof(Views.LoginPage));
        Routing.RegisterRoute(nameof(Views.RegisterPage), typeof(Views.RegisterPage));
        Routing.RegisterRoute(nameof(Views.TempPage), typeof(Views.TempPage));
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var shell = _serviceProvider.GetService<AppShell>();
        if (shell == null)
            throw new InvalidOperationException(
                "AppShell could not be resolved from the service provider."
            );

        return new Window(shell);
    }

    protected override async void OnStart()
    {
        base.OnStart();

        var credentialStore = _serviceProvider.GetRequiredService<ICredentialStore>();
        var authService = _serviceProvider.GetRequiredService<IAuthenticationService>();
        var navigationService = _serviceProvider.GetRequiredService<INavigationService>();

        var credentials = await credentialStore.GetAsync();
        if (credentials is null)
            return;

        var result = await authService.LoginAsync(
            credentials.Value.Email,
            credentials.Value.Password
        );

        if (result.IsSuccess)
            await navigationService.NavigateToAsync("MainPage");
    }
}
