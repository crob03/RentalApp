namespace RentalApp.Services;

/// <summary>
/// Thrown by <see cref="AuthRefreshHandler"/> when a token refresh attempt fails and the session
/// cannot be recovered. Caught centrally by <see cref="ApiClient"/>, which navigates to the login
/// route before the exception reaches any ViewModel or service.
/// </summary>
public class AuthenticationExpiredException : Exception
{
    /// <summary>
    /// Initialises a new instance of <see cref="AuthenticationExpiredException"/>.
    /// </summary>
    public AuthenticationExpiredException()
        : base("The authentication session has expired and could not be refreshed.") { }
}
