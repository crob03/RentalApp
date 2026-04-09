using RentalApp.ViewModels;

namespace RentalApp;

/// <summary>
/// The application shell, responsible for top-level navigation structure and flyout hosting.
/// Binds to <see cref="AppShellViewModel"/> for navigation commands and authentication-aware
/// menu state.
/// </summary>
public partial class AppShell : Shell
{
    /// <summary>
    /// Initialises a new instance of <see cref="AppShell"/> and sets its binding context.
    /// </summary>
    /// <param name="viewModel">The view model to bind to this shell.</param>
    public AppShell(AppShellViewModel viewModel)
    {
        BindingContext = viewModel;
        InitializeComponent();
    }
}
