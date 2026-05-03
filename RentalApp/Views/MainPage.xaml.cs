using RentalApp.ViewModels;

namespace RentalApp.Views;

/// <summary>
/// Authenticated dashboard page — the root route navigated to after a successful login.
/// </summary>
public partial class MainPage : ContentPage
{
    private readonly MainViewModel _viewModel;

    /// <summary>
    /// Initialises the page and binds it to the provided <see cref="MainViewModel"/>.
    /// </summary>
    /// <param name="viewModel">The transient view model managing dashboard state and commands.</param>
    public MainPage(MainViewModel viewModel)
    {
        _viewModel = viewModel;
        BindingContext = viewModel;
        InitializeComponent();
    }

    /// <summary>Loads the current-user profile each time the page appears.</summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }
}
