using Microsoft.Extensions.Logging;
using RentalApp.Database.Data;
using RentalApp.Services;
using RentalApp.ViewModels;
using RentalApp.Views;

namespace RentalApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddSingleton<ICredentialStore, SecureCredentialStore>();

        bool useSharedApi = false;

        if (useSharedApi)
        {
            var baseAddress = new Uri("https://set09102-api.b-davison.workers.dev/");

            builder.Services.AddSingleton<AuthTokenState>();
            builder.Services.AddSingleton(sp => new AuthRefreshHandler(
                sp.GetRequiredService<AuthTokenState>(),
                sp.GetRequiredService<ICredentialStore>(),
                sp.GetRequiredService<INavigationService>(),
                baseAddress
            )
            {
                InnerHandler = new HttpClientHandler(),
            });
            builder.Services.AddSingleton(sp => new HttpClient(
                sp.GetRequiredService<AuthRefreshHandler>()
            )
            {
                BaseAddress = baseAddress,
            });
            builder.Services.AddSingleton<IAuthenticationService, ApiAuthenticationService>();
        }
        else
        {
            builder.Services.AddDbContext<AppDbContext>();
            builder.Services.AddSingleton<IAuthenticationService, LocalAuthenticationService>();
        }

        builder.Services.AddSingleton<INavigationService, NavigationService>();

        builder.Services.AddSingleton<AppShellViewModel>();
        builder.Services.AddSingleton<AppShell>();
        builder.Services.AddSingleton<App>();

        builder.Services.AddTransient<MainViewModel>();
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddSingleton<LoginViewModel>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddSingleton<RegisterViewModel>();
        builder.Services.AddTransient<RegisterPage>();
        builder.Services.AddSingleton<TempViewModel>();
        builder.Services.AddTransient<TempPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
