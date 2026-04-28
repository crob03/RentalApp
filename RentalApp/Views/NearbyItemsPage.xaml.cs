using RentalApp.ViewModels;

namespace RentalApp.Views;

public partial class NearbyItemsPage : ContentPage
{
    public NearbyItemsViewModel ViewModel { get; }

    public NearbyItemsPage(NearbyItemsViewModel viewModel)
    {
        ViewModel = viewModel;
        BindingContext = viewModel;
        InitializeComponent();
    }
}
