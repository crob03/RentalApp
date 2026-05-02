using RentalApp.ViewModels;

namespace RentalApp.Views;

/// <summary>
/// Nearby-items page. Acquires device location and loads items within the configured radius on appearance.
/// </summary>
public partial class NearbyItemsPage : ContentPage
{
    private NearbyItemsViewModel ViewModel => (NearbyItemsViewModel)BindingContext;

    /// <summary>
    /// Initialises the page and binds it to the provided <see cref="NearbyItemsViewModel"/>.
    /// </summary>
    /// <param name="viewModel">The transient view model managing nearby-item state and commands.</param>
    public NearbyItemsPage(NearbyItemsViewModel viewModel)
    {
        BindingContext = viewModel;
        InitializeComponent();
    }

    /// <summary>Fires <see cref="NearbyItemsViewModel.LoadNearbyItemsCommand"/> each time the page appears.</summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ViewModel.LoadNearbyItemsCommand.ExecuteAsync(null);
    }
}
