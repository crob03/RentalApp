namespace RentalApp.Http;

/// <summary>
/// Provides HTTP operations for communicating with the remote API.
/// Implementations are responsible for redirecting to the login route on session expiry
/// so callers are never exposed to authentication failures.
/// </summary>
public interface IApiClient
{
    /// <summary>
    /// Sends a GET request to the specified <paramref name="requestUri"/>.
    /// </summary>
    /// <param name="requestUri">The relative or absolute URI of the request.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The HTTP response message.</returns>
    Task<HttpResponseMessage> GetAsync(
        string requestUri,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Sends a POST request with <paramref name="value"/> serialised as JSON to the specified
    /// <paramref name="requestUri"/>.
    /// </summary>
    /// <typeparam name="T">The type of the value to serialise.</typeparam>
    /// <param name="requestUri">The relative or absolute URI of the request.</param>
    /// <param name="value">The value to serialise as the request body.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The HTTP response message.</returns>
    Task<HttpResponseMessage> PostAsJsonAsync<T>(
        string requestUri,
        T value,
        CancellationToken cancellationToken = default
    );
}
