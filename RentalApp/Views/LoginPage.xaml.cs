using RentalApp.ViewModels;

namespace RentalApp.Views;

/// <summary>
/// The login page. Entry point of the application — presented at the <c>//login</c> root route.
/// </summary>
public partial class LoginPage : ContentPage
{
    private LoginViewModel ViewModel => (LoginViewModel)BindingContext;

    /// <summary>
    /// Initialises the page and binds it to the provided <see cref="LoginViewModel"/>.
    /// </summary>
    /// <param name="viewModel">The singleton view model managing login state and commands.</param>
    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    /// <summary>
    /// Restores saved credentials into the form and focuses the email entry field.
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ViewModel.InitializeAsync();
        EmailEntry.Focus();
    }
}
