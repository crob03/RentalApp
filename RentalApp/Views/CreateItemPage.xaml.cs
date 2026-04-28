using RentalApp.ViewModels;

namespace RentalApp.Views;

public partial class CreateItemPage : ContentPage
{
    public CreateItemViewModel ViewModel { get; }

    public CreateItemPage(CreateItemViewModel viewModel)
    {
        ViewModel = viewModel;
        BindingContext = viewModel;
        InitializeComponent();
    }
}
