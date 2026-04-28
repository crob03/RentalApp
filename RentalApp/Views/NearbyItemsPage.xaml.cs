using RentalApp.ViewModels;

namespace RentalApp.Views;

public partial class NearbyItemsPage : ContentPage
{
    private NearbyItemsViewModel ViewModel => (NearbyItemsViewModel)BindingContext;

    public NearbyItemsPage(NearbyItemsViewModel viewModel)
    {
        BindingContext = viewModel;
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ViewModel.LoadNearbyItemsCommand.ExecuteAsync(null);
    }
}
