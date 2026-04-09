using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace RentalApp.ViewModels;

/// <summary>
/// Abstract base class for all view models in the application.
/// Extends <see cref="ObservableObject"/> and provides shared observable properties for busy state,
/// page title, and error handling, along with helper methods consumed by derived view models.
/// </summary>
public partial class BaseViewModel : ObservableObject
{
    /// <summary>
    /// Indicates whether the view model is currently executing an asynchronous operation.
    /// </summary>
    [ObservableProperty]
    private bool isBusy;

    /// <summary>
    /// The display title for the current page or view.
    /// </summary>
    [ObservableProperty]
    private string title = string.Empty;

    /// <summary>
    /// The current error message to display to the user.
    /// </summary>
    [ObservableProperty]
    private string errorMessage = string.Empty;

    /// <summary>
    /// Indicates whether there is an active error that should be shown to the user.
    /// </summary>
    [ObservableProperty]
    private bool hasError;

    /// <summary>
    /// Sets the error state with the supplied message and marks <see cref="HasError"/> as
    /// <see langword="true"/>.
    /// </summary>
    /// <param name="message">The error message to display.</param>
    protected void SetError(string message)
    {
        ErrorMessage = message;
        HasError = !string.IsNullOrEmpty(message);
    }

    /// <summary>
    /// Clears the current error state, resetting both <see cref="ErrorMessage"/> and
    /// <see cref="HasError"/>.
    /// </summary>
    protected void ClearError()
    {
        ErrorMessage = string.Empty;
        HasError = false;
    }

    /// <summary>
    /// Command that clears the current error state. Intended for binding to dismiss-error
    /// controls in the UI.
    /// </summary>
    [RelayCommand]
    private void ClearErrorCommand()
    {
        ClearError();
    }
}
