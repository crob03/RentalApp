using System.Net;
using System.Net.Http.Json;
using System.Text;
using RentalApp.Constants;

namespace RentalApp.Services;

/// <summary>
/// Implements <see cref="IApiClient"/> by delegating to an <see cref="HttpClient"/> whose pipeline
/// includes <see cref="AuthRefreshHandler"/>.
/// Catches <see cref="AuthenticationExpiredException"/> thrown by the handler when a token refresh
/// fails, navigates to the login route, and returns an empty 401 response so callers can handle
/// the failure path without knowledge of the session-expiry event.
/// </summary>
public class ApiClient : IApiClient
{
    private readonly HttpClient _httpClient;
    private readonly INavigationService _navigationService;

    /// <summary>
    /// Initialises a new instance of <see cref="ApiClient"/>.
    /// </summary>
    /// <param name="httpClient">The HTTP client whose pipeline includes <see cref="AuthRefreshHandler"/>.</param>
    /// <param name="navigationService">The navigation service used to redirect to login on session expiry.</param>
    public ApiClient(HttpClient httpClient, INavigationService navigationService)
    {
        _httpClient = httpClient;
        _navigationService = navigationService;
    }

    /// <inheritdoc/>
    public async Task<HttpResponseMessage> GetAsync(
        string requestUri,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            return await _httpClient.GetAsync(requestUri, cancellationToken);
        }
        catch (AuthenticationExpiredException)
        {
            await _navigationService.NavigateToAsync(
                Routes.Login,
                new Dictionary<string, object> { ["sessionExpired"] = true }
            );
            return SessionExpiredResponse();
        }
    }

    /// <inheritdoc/>
    public async Task<HttpResponseMessage> PostAsJsonAsync<T>(
        string requestUri,
        T value,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            return await _httpClient.PostAsJsonAsync(requestUri, value, cancellationToken);
        }
        catch (AuthenticationExpiredException)
        {
            await _navigationService.NavigateToAsync(
                Routes.Login,
                new Dictionary<string, object> { ["sessionExpired"] = true }
            );
            return SessionExpiredResponse();
        }
    }

    /// <summary>
    /// Returns a minimal 401 response used as a sentinel after session-expiry navigation.
    /// Callers treat it as a regular failure — the user has already been redirected to login.
    /// </summary>
    private static HttpResponseMessage SessionExpiredResponse() =>
        new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json"),
        };
}
