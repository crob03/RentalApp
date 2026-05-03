using RentalApp.ViewModels;

namespace RentalApp.Views;

/// <summary>
/// Rentals page. Loads the current user's incoming or outgoing rentals on appearance.
/// </summary>
public partial class RentalsPage : ContentPage
{
    /// <summary>The bound view model, also accessible from XAML via direct element binding.</summary>
    public RentalsViewModel ViewModel { get; }

    /// <summary>
    /// Initialises the page and binds it to the provided <see cref="RentalsViewModel"/>.
    /// </summary>
    /// <param name="viewModel">The transient view model managing rental list state.</param>
    public RentalsPage(RentalsViewModel viewModel)
    {
        ViewModel = viewModel;
        BindingContext = viewModel;
        InitializeComponent();
    }

    /// <summary>Fires <see cref="RentalsViewModel.LoadRentalsCommand"/> each time the page appears.</summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        ViewModel.LoadRentalsCommand.Execute(null);
    }
}
