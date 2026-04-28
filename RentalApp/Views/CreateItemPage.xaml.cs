using RentalApp.ViewModels;

namespace RentalApp.Views;

public partial class CreateItemPage : ContentPage
{
    private CreateItemViewModel ViewModel => (CreateItemViewModel)BindingContext;

    public CreateItemPage(CreateItemViewModel viewModel)
    {
        BindingContext = viewModel;
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ViewModel.LoadCategoriesCommand.ExecuteAsync(null);
    }
}
