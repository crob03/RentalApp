// RentalApp/MauiProgram.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Devices.Sensors;
using RentalApp.Database.Data;
using RentalApp.Database.Repositories;
using RentalApp.Http;
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

        builder.Services.AddSingleton<ICredentialStore, CredentialStore>();
        builder.Services.AddSingleton<AuthTokenState>();

        bool useSharedApi = Preferences.Default.Get("UseSharedApi", true);

        if (useSharedApi)
        {
            var baseAddress = new Uri("https://set09102-api.b-davison.workers.dev/");
            builder.Services.AddSingleton(sp => new HttpClient { BaseAddress = baseAddress });
            builder.Services.AddSingleton<IApiClient, ApiClient>();
            builder.Services.AddSingleton<IApiService, RemoteApiService>();
        }
        else
        {
            builder.Services.AddDbContextFactory<AppDbContext>();
            builder.Services.AddSingleton<IItemRepository, ItemRepository>();
            builder.Services.AddSingleton<ICategoryRepository, CategoryRepository>();
            builder.Services.AddSingleton<IApiService>(sp => new LocalApiService(
                sp.GetRequiredService<IDbContextFactory<AppDbContext>>(),
                sp.GetRequiredService<IItemRepository>(),
                sp.GetRequiredService<ICategoryRepository>(),
                sp.GetRequiredService<AuthTokenState>()
            ));
        }

        builder.Services.AddSingleton<IAuthenticationService>(sp => new AuthenticationService(
            sp.GetRequiredService<IApiService>(),
            sp.GetRequiredService<ICredentialStore>(),
            sp.GetRequiredService<AuthTokenState>()
        ));

        builder.Services.AddSingleton<IGeolocation>(Geolocation.Default);
        builder.Services.AddSingleton<ILocationService, LocationService>();
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

        builder.Services.AddTransient<ItemsListViewModel>();
        builder.Services.AddTransient<ItemsListPage>();
        builder.Services.AddTransient<ItemDetailsViewModel>();
        builder.Services.AddTransient<ItemDetailsPage>();
        builder.Services.AddTransient<CreateItemViewModel>();
        builder.Services.AddTransient<CreateItemPage>();
        builder.Services.AddTransient<NearbyItemsViewModel>();
        builder.Services.AddTransient<NearbyItemsPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
