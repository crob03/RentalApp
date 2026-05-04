using RentalApp.ViewModels;

namespace RentalApp.Views;

public partial class UserProfilePage : ContentPage
{
    private UserProfileViewModel ViewModel => (UserProfileViewModel)BindingContext;

    public UserProfilePage(UserProfileViewModel viewModel)
    {
        BindingContext = viewModel;
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ViewModel.LoadProfileCommand.ExecuteAsync(null);
    }
}
