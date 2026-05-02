using System.Net.Http.Json;

namespace RentalApp.Services;

/// <summary>
/// Abstract base for HTTP service implementations, providing shared error-handling over <see cref="HttpResponseMessage"/>.
/// </summary>
internal abstract class RemoteServiceBase
{
    /// <summary>
    /// Throws an <see cref="HttpRequestException"/> if <paramref name="response"/> does not indicate success,
    /// deserialising the API error body for a human-readable message where available.
    /// </summary>
    /// <param name="response">The HTTP response to inspect.</param>
    /// <exception cref="HttpRequestException">Thrown when the response status code indicates a failure.</exception>
    protected static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        throw new HttpRequestException(
            error?.Message ?? $"Request failed with status {(int)response.StatusCode}"
        );
    }

    protected sealed record ApiErrorResponse(string Error, string Message);
}
