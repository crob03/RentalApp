using RentalApp.ViewModels;

namespace RentalApp.Views;

/// <summary>
/// Code-behind for the user-profile page. Triggers profile and review loading on each appearance.
/// </summary>
public partial class UserProfilePage : ContentPage
{
    private UserProfileViewModel ViewModel => (UserProfileViewModel)BindingContext;

    /// <summary>Initialises the page and sets the binding context.</summary>
    public UserProfilePage(UserProfileViewModel viewModel)
    {
        BindingContext = viewModel;
        InitializeComponent();
    }

    /// <inheritdoc/>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ViewModel.LoadProfileCommand.ExecuteAsync(null);
    }
}
