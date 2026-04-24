using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using RentalApp.Constants;
using RentalApp.Services;

namespace RentalApp.Http;

/// <summary>
/// Implements <see cref="IApiClient"/> by delegating to an <see cref="HttpClient"/>.
/// Attaches the current bearer token to every outgoing request and navigates to the
/// login route when a 401 Unauthorized response is received.
/// </summary>
public class ApiClient : IApiClient
{
    private readonly HttpClient _httpClient;
    private readonly AuthTokenState _tokenState;
    private readonly INavigationService _navigationService;
    private readonly ILogger<ApiClient> _logger;

    /// <summary>
    /// Initialises a new instance of <see cref="ApiClient"/>.
    /// </summary>
    /// <param name="httpClient">The underlying HTTP client.</param>
    /// <param name="tokenState">The shared token state providing the current bearer token.</param>
    /// <param name="navigationService">The navigation service used to redirect to login on session expiry.</param>
    /// <param name="logger">Logger for HTTP request and response diagnostics.</param>
    public ApiClient(
        HttpClient httpClient,
        AuthTokenState tokenState,
        INavigationService navigationService,
        ILogger<ApiClient> logger
    )
    {
        _httpClient = httpClient;
        _tokenState = tokenState;
        _navigationService = navigationService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<HttpResponseMessage> GetAsync(
        string requestUri,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogDebug("GET {Uri}", requestUri);
        var request = CreateRequest(HttpMethod.Get, requestUri);
        var sentWithToken = request.Headers.Authorization != null;
        var response = await _httpClient.SendAsync(request, cancellationToken);
        _logger.LogDebug("GET {Uri} → {StatusCode}", requestUri, (int)response.StatusCode);
        return sentWithToken && response.StatusCode == HttpStatusCode.Unauthorized
            ? await HandleSessionExpiredAsync(response)
            : response;
    }

    /// <inheritdoc/>
    public async Task<HttpResponseMessage> PostAsJsonAsync<T>(
        string requestUri,
        T value,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogDebug("POST {Uri}", requestUri);
        var request = CreateRequest(HttpMethod.Post, requestUri);
        request.Content = JsonContent.Create(value);
        var sentWithToken = request.Headers.Authorization != null;
        var response = await _httpClient.SendAsync(request, cancellationToken);
        _logger.LogDebug("POST {Uri} → {StatusCode}", requestUri, (int)response.StatusCode);
        return sentWithToken && response.StatusCode == HttpStatusCode.Unauthorized
            ? await HandleSessionExpiredAsync(response)
            : response;
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string requestUri)
    {
        var request = new HttpRequestMessage(method, requestUri);
        if (_tokenState.CurrentToken != null)
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                _tokenState.CurrentToken
            );
        return request;
    }

    private async Task<HttpResponseMessage> HandleSessionExpiredAsync(HttpResponseMessage response)
    {
        _logger.LogWarning("Session expired — redirecting to login");
        await _navigationService.NavigateToAsync(
            Routes.Login,
            new Dictionary<string, object> { ["sessionExpired"] = true }
        );
        return response;
    }
}
