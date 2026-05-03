using RentalApp.Constants;
using RentalApp.Views;

namespace RentalApp;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(Routes.Register, typeof(RegisterPage));
        Routing.RegisterRoute(Routes.Temp, typeof(TempPage));
        Routing.RegisterRoute(Routes.ItemsList, typeof(ItemsListPage));
        Routing.RegisterRoute(Routes.ItemDetails, typeof(ItemDetailsPage));
        Routing.RegisterRoute(Routes.CreateItem, typeof(CreateItemPage));
        Routing.RegisterRoute(Routes.NearbyItems, typeof(NearbyItemsPage));
    }
}
