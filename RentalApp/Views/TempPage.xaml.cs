using RentalApp.ViewModels;

namespace RentalApp.Views;

/// <summary>
/// Legacy stub page used as a placeholder for post-login screens still under development.
/// </summary>
public partial class TempPage : ContentPage
{
    /// <summary>
    /// Initialises the page and binds it to the provided <see cref="TempViewModel"/>.
    /// </summary>
    /// <param name="viewModel">The singleton placeholder view model.</param>
    public TempPage(TempViewModel viewModel)
    {
        BindingContext = viewModel;
        InitializeComponent();
    }
}
