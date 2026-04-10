using RentalApp.ViewModels;

namespace RentalApp.Views;

/// <summary>
/// The main authenticated page, presented after a successful login or registration.
/// </summary>
public partial class MainPage : ContentPage
{
    /// <summary>
    /// Initialises the page and binds it to the provided <see cref="MainViewModel"/>.
    /// </summary>
    /// <param name="viewModel">The transient view model for this page.</param>
    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
