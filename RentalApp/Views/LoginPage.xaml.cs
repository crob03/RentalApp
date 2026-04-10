using RentalApp.ViewModels;

namespace RentalApp.Views;

/// <summary>
/// The login page. Entry point of the application — presented at the <c>//login</c> root route.
/// </summary>
public partial class LoginPage : ContentPage
{
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
    /// Focuses the email entry field when the page becomes visible.
    /// </summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        EmailEntry.Focus();
    }
}
