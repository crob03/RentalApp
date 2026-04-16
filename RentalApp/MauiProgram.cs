using Microsoft.Extensions.Logging;
using RentalApp.Database.Data;
using RentalApp.Services;
using RentalApp.ViewModels;
using RentalApp.Views;

namespace RentalApp;

/// <summary>
/// Entry point for the MAUI application. Configures the dependency injection container,
/// registers all services, view models, and pages, and builds the <see cref="MauiApp"/>.
/// </summary>
public static class MauiProgram
{
    /// <summary>
    /// Creates and configures the <see cref="MauiApp"/> instance.
    /// Toggles between <see cref="ApiAuthenticationService"/> and
    /// <see cref="LocalAuthenticationService"/> via the <c>useSharedApi</c> flag.
    /// </summary>
    /// <returns>The configured <see cref="MauiApp"/>.</returns>
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

        bool useSharedApi = Preferences.Default.Get("UseSharedApi", true);

        if (useSharedApi)
        {
            var baseAddress = new Uri("https://set09102-api.b-davison.workers.dev/");

            builder.Services.AddSingleton<AuthTokenState>();
            builder.Services.AddSingleton(sp => new AuthRefreshHandler(
                sp.GetRequiredService<AuthTokenState>(),
                sp.GetRequiredService<ICredentialStore>(),
                baseAddress,
                sp.GetRequiredService<ILogger<AuthRefreshHandler>>()
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
            builder.Services.AddSingleton<IApiClient>(sp => new ApiClient(
                sp.GetRequiredService<HttpClient>(),
                sp.GetRequiredService<INavigationService>()
            ));
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

        builder.Services.AddTransient<LoadingViewModel>();
        builder.Services.AddTransient<LoadingPage>();
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
