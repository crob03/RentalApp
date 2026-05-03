using Microsoft.Extensions.Logging;
using RentalApp.Database.Data;
using RentalApp.Database.Repositories;
using RentalApp.Http;
using RentalApp.Services.Auth;
using RentalApp.Services.Items;
using RentalApp.Services.Location;
using RentalApp.Services.Navigation;
using RentalApp.Services.Rentals;
using RentalApp.Services.Reviews;
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

#if DEBUG
        bool useSharedApi = Preferences.Default.Get("UseSharedApi", true);
#else
        const bool useSharedApi = true;
#endif

        if (useSharedApi)
        {
            var baseAddress = new Uri("https://set09102-api.b-davison.workers.dev/");
            builder.Services.AddSingleton(sp => new HttpClient { BaseAddress = baseAddress });
            builder.Services.AddSingleton<IApiClient, ApiClient>();
            builder.Services.AddSingleton<IAuthService, RemoteAuthService>();
            builder.Services.AddSingleton<IItemService, RemoteItemService>();
            builder.Services.AddSingleton<IRentalService, RemoteRentalService>();
            builder.Services.AddSingleton<IReviewService, RemoteReviewService>();
        }
        else
        {
            builder.Services.AddDbContextFactory<AppDbContext>();
            builder.Services.AddSingleton<IUserRepository, UserRepository>();
            builder.Services.AddSingleton<IItemRepository, ItemRepository>();
            builder.Services.AddSingleton<ICategoryRepository, CategoryRepository>();
            builder.Services.AddSingleton<IAuthService>(sp => new LocalAuthService(
                sp.GetRequiredService<IUserRepository>(),
                sp.GetRequiredService<IItemRepository>(),
                sp.GetRequiredService<AuthTokenState>()
            ));
            builder.Services.AddSingleton<IItemService>(sp => new LocalItemService(
                sp.GetRequiredService<IItemRepository>(),
                sp.GetRequiredService<ICategoryRepository>(),
                sp.GetRequiredService<AuthTokenState>()
            ));
            builder.Services.AddSingleton<IRentalService, LocalRentalService>();
            builder.Services.AddSingleton<IReviewService, LocalReviewService>();
        }

        builder.Services.AddSingleton<IGeolocation>(Geolocation.Default);
        builder.Services.AddSingleton<ILocationService, LocationService>();
        builder.Services.AddSingleton<INavigationService, NavigationService>();

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
