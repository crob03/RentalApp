using RentalApp.ViewModels;

namespace RentalApp.Views;

/// <summary>
/// The user registration page.
/// </summary>
public partial class RegisterPage : ContentPage
{
    /// <summary>
    /// Initialises the page and binds it to the provided <see cref="RegisterViewModel"/>.
    /// </summary>
    /// <param name="viewModel">The singleton view model managing registration state and commands.</param>
    public RegisterPage(RegisterViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
