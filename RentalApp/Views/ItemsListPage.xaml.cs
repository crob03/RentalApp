using RentalApp.ViewModels;

namespace RentalApp.Views;

/// <summary>
/// Browseable item-listing page. Triggers an initial item load on appearance and exposes
/// <see cref="ViewModel"/> for direct element binding in XAML.
/// </summary>
public partial class ItemsListPage : ContentPage
{
    /// <summary>The bound view model, also accessible from XAML via direct element binding.</summary>
    public ItemsListViewModel ViewModel { get; }

    /// <summary>
    /// Initialises the page and binds it to the provided <see cref="ItemsListViewModel"/>.
    /// </summary>
    /// <param name="viewModel">The transient view model managing item list state and commands.</param>
    public ItemsListPage(ItemsListViewModel viewModel)
    {
        ViewModel = viewModel;
        BindingContext = viewModel;
        InitializeComponent();
    }

    /// <summary>Fires <see cref="ItemsListViewModel.LoadItemsCommand"/> each time the page appears.</summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        ViewModel.LoadItemsCommand.Execute(null);
    }
}
