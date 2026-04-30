using RentalApp.ViewModels;

namespace RentalApp.Views;

/// <summary>
/// A transient splash/loading page shown on startup while session state is being determined.
/// </summary>
public partial class LoadingPage : ContentPage
{
    private LoadingViewModel ViewModel => (LoadingViewModel)BindingContext;

    /// <summary>
    /// Initialises the page and binds it to the provided <see cref="LoadingViewModel"/>.
    /// </summary>
    /// <param name="viewModel">The transient view model responsible for startup initialisation logic.</param>
    public LoadingPage(LoadingViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    /// <summary>
    /// Triggers <see cref="LoadingViewModel.InitializeAsync"/> when the page appears,
    /// which evaluates persisted credentials and navigates accordingly.
    /// </summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ViewModel.InitializeAsync();
    }
}
