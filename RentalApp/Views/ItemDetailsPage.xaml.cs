using RentalApp.ViewModels;

namespace RentalApp.Views;

public partial class ItemDetailsPage : ContentPage
{
    public ItemDetailsViewModel ViewModel { get; }

    public ItemDetailsPage(ItemDetailsViewModel viewModel)
    {
        ViewModel = viewModel;
        BindingContext = viewModel;
        InitializeComponent();
    }
}
