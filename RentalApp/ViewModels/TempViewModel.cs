using RentalApp.Http;
using RentalApp.Services;

namespace RentalApp.ViewModels;

/// <summary>
/// Placeholder view model used by temporary stub pages during development.
/// </summary>
public partial class TempViewModel : AuthenticatedViewModel
{
    /// <summary>
    /// Gets the application version string as reported by <see cref="AppInfo"/>.
    /// </summary>
    public string Version => AppInfo.VersionString;

    /// <summary>
    /// Gets a message indicating that this page is a placeholder.
    /// </summary>
    public string Message => "This is a placeholder page.";

    /// <summary>
    /// Initialises the view model with authentication and navigation dependencies.
    /// </summary>
    /// <param name="tokenState">Passed to <see cref="AuthenticatedViewModel"/>.</param>
    /// <param name="credentialStore">Passed to <see cref="AuthenticatedViewModel"/>.</param>
    /// <param name="navigationService">Passed to <see cref="AuthenticatedViewModel"/>.</param>
    public TempViewModel(
        AuthTokenState tokenState,
        ICredentialStore credentialStore,
        INavigationService navigationService
    )
        : base(tokenState, credentialStore, navigationService)
    {
        Title = AppInfo.Name;
    }
}
