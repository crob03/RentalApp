using RentalApp.ViewModels;

namespace RentalApp.Views;

public partial class LoadingPage : ContentPage
{
    public LoadingPage(LoadingViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ((LoadingViewModel)BindingContext).InitializeAsync();
    }
}
