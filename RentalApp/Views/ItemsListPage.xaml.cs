using RentalApp.ViewModels;

namespace RentalApp.Views;

public partial class ItemsListPage : ContentPage
{
    public ItemsListViewModel ViewModel { get; }

    public ItemsListPage(ItemsListViewModel viewModel)
    {
        ViewModel = viewModel;
        BindingContext = viewModel;
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ViewModel.LoadItemsCommand.Execute(null);
    }
}
