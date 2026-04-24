using CommunityToolkit.Mvvm.Input;

namespace RentalApp.ViewModels;

/// <summary>
/// Placeholder view model used by temporary stub pages during development.
/// </summary>
public class TempViewModel
{
    /// <summary>
    /// Gets the application name as reported by <see cref="AppInfo"/>.
    /// </summary>
    public string Title => AppInfo.Name;

    /// <summary>
    /// Gets the application version string as reported by <see cref="AppInfo"/>.
    /// </summary>
    public string Version => AppInfo.VersionString;

    /// <summary>
    /// Gets a message indicating that this page is a placeholder.
    /// </summary>
    public string Message => "This is a placeholder page.";

    /// <summary>
    /// Initialises a new instance of <see cref="TempViewModel"/>.
    /// </summary>
    public TempViewModel() { }
}
