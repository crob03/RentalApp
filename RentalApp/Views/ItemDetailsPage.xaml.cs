using RentalApp.ViewModels;

namespace RentalApp.Views;

/// <summary>
/// Item-details page. Receives the item ID via Shell query attributes and loads the full item on appearance.
/// </summary>
public partial class ItemDetailsPage : ContentPage
{
    private ItemDetailsViewModel ViewModel => (ItemDetailsViewModel)BindingContext;

    /// <summary>
    /// Initialises the page and binds it to the provided <see cref="ItemDetailsViewModel"/>.
    /// </summary>
    /// <param name="viewModel">The transient view model managing item detail state and commands.</param>
    public ItemDetailsPage(ItemDetailsViewModel viewModel)
    {
        BindingContext = viewModel;
        InitializeComponent();
    }

    /// <summary>Fires <see cref="ItemDetailsViewModel.LoadItemCommand"/> each time the page appears.</summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ViewModel.LoadItemCommand.ExecuteAsync(null);
    }
}
