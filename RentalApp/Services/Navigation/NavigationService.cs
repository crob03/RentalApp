namespace RentalApp.Services.Navigation;

/// <summary>
/// Implements <see cref="INavigationService"/> using .NET MAUI's <see cref="Shell"/> for
/// route-based navigation.
/// </summary>
public class NavigationService : INavigationService
{
    /// <inheritdoc/>
    public async Task NavigateToAsync(string route)
    {
        await Shell.Current.GoToAsync(route);
    }

    /// <inheritdoc/>
    public async Task NavigateToAsync(string route, Dictionary<string, object> parameters)
    {
        await Shell.Current.GoToAsync(route, parameters);
    }

    /// <inheritdoc/>
    public async Task NavigateBackAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    /// <inheritdoc/>
    public async Task PopToRootAsync()
    {
        await Shell.Current.Navigation.PopToRootAsync();
    }
}
