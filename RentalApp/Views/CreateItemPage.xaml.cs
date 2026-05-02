using RentalApp.ViewModels;

namespace RentalApp.Views;

/// <summary>
/// Create-item page. Loads available categories on appearance and submits a new listing on command.
/// </summary>
public partial class CreateItemPage : ContentPage
{
    private CreateItemViewModel ViewModel => (CreateItemViewModel)BindingContext;

    /// <summary>
    /// Initialises the page and binds it to the provided <see cref="CreateItemViewModel"/>.
    /// </summary>
    /// <param name="viewModel">The transient view model managing item-creation state and commands.</param>
    public CreateItemPage(CreateItemViewModel viewModel)
    {
        BindingContext = viewModel;
        InitializeComponent();
    }

    /// <summary>Fires <see cref="CreateItemViewModel.LoadCategoriesCommand"/> each time the page appears.</summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ViewModel.LoadCategoriesCommand.ExecuteAsync(null);
    }
}
