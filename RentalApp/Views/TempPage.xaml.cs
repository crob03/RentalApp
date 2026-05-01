using RentalApp.ViewModels;

namespace RentalApp.Views;

public partial class TempPage : ContentPage
{
    public TempPage(TempViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
