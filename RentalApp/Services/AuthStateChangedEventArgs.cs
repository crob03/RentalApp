using RentalApp.Database.Models;

namespace RentalApp.Services;

/// <summary>
/// Provides data for authentication state change events.
/// </summary>
public class AuthStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets a value indicating whether the user is now authenticated.
    /// </summary>
    public bool IsAuthenticated { get; set; }

    /// <summary>
    /// Gets or sets the authenticated user, or <see langword="null"/> when the user has logged out.
    /// </summary>
    public User? User { get; set; }
}
