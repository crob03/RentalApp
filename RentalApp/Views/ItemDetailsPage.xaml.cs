using RentalApp.ViewModels;

namespace RentalApp.Views;

public partial class ItemDetailsPage : ContentPage
{
    private ItemDetailsViewModel ViewModel => (ItemDetailsViewModel)BindingContext;

    public ItemDetailsPage(ItemDetailsViewModel viewModel)
    {
        BindingContext = viewModel;
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ViewModel.LoadItemCommand.ExecuteAsync(null);
    }
}
