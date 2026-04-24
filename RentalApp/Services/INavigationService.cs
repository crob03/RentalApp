namespace RentalApp.Services;

/// <summary>
/// Defines the contract for Shell-based page navigation.
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Navigates to the specified route.
    /// </summary>
    /// <param name="route">The Shell route to navigate to.</param>
    Task NavigateToAsync(string route);

    /// <summary>
    /// Navigates to the specified route, passing query parameters to the destination page.
    /// </summary>
    /// <param name="route">The Shell route to navigate to.</param>
    /// <param name="parameters">A dictionary of query parameters to pass to the destination.</param>
    Task NavigateToAsync(string route, Dictionary<string, object> parameters);

    /// <summary>
    /// Navigates back one step in the navigation stack.
    /// </summary>
    Task NavigateBackAsync();

    /// <summary>
    /// Pops all pages above the root from the navigation stack.
    /// </summary>
    Task PopToRootAsync();
}
