using RentalApp.ViewModels;

namespace RentalApp.Views;

/// <summary>
/// Manage-rental page. Receives the rental ID via Shell query attributes and loads the rental on appearance.
/// </summary>
public partial class ManageRentalPage : ContentPage
{
    private ManageRentalViewModel ViewModel => (ManageRentalViewModel)BindingContext;

    /// <summary>
    /// Initialises the page and binds it to the provided <see cref="ManageRentalViewModel"/>.
    /// </summary>
    /// <param name="viewModel">The transient view model managing rental detail state and commands.</param>
    public ManageRentalPage(ManageRentalViewModel viewModel)
    {
        BindingContext = viewModel;
        InitializeComponent();
    }

    /// <summary>Fires <see cref="ManageRentalViewModel.LoadRentalCommand"/> each time the page appears.</summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ViewModel.LoadRentalCommand.ExecuteAsync(null);
    }
}
