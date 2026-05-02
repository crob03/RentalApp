using System.Net.Http.Json;

namespace RentalApp.Services;

internal abstract class RemoteServiceBase
{
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
