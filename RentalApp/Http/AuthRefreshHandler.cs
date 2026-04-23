using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using RentalApp.Exceptions;
using RentalApp.Services;

namespace RentalApp.Http;

/// <summary>
/// A <see cref="DelegatingHandler"/> that attaches the current bearer token to outgoing requests
/// and transparently retries a request once after refreshing the token when a 401 Unauthorized
/// response is received.
/// </summary>
public class AuthRefreshHandler : DelegatingHandler
{
    private static readonly HttpRequestOptionsKey<bool> IsRetryKey = new("IsRetry");

    private readonly AuthTokenState _tokenState;
    private readonly ICredentialStore _credentialStore;
    private readonly Uri _baseAddress;
    private readonly ILogger<AuthRefreshHandler> _logger;

    /// <summary>
    /// Initialises a new instance of <see cref="AuthRefreshHandler"/>.
    /// </summary>
    /// <param name="tokenState">The shared token state providing the current bearer token.</param>
    /// <param name="credentialStore">The credential store used to retrieve saved credentials for token refresh.</param>
    /// <param name="baseAddress">The base URI of the API, used when constructing the token refresh request.</param>
    /// <param name="logger">The logger for this handler.</param>
    public AuthRefreshHandler(
        AuthTokenState tokenState,
        ICredentialStore credentialStore,
        Uri baseAddress,
        ILogger<AuthRefreshHandler> logger
    )
    {
        _tokenState = tokenState;
        _credentialStore = credentialStore;
        _baseAddress = baseAddress;
        _logger = logger;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Attaches the current bearer token if present, sends the request, then attempts a single
    /// token refresh and retry if a 401 is returned.
    /// </remarks>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        // Buffer content now so it can be re-read if we need to retry
        if (request.Content != null)
            await request.Content.LoadIntoBufferAsync(cancellationToken);

        if (_tokenState.CurrentToken != null && request.Headers.Authorization == null)
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                _tokenState.CurrentToken
            );

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            request.Options.TryGetValue(IsRetryKey, out var isRetry);
            if (!isRetry)
                response = await HandleUnauthorizedAsync(request, cancellationToken);
        }

        return response;
    }

    /// <summary>
    /// Attempts to refresh the bearer token using stored credentials and retries the original
    /// request. Throws <see cref="AuthenticationExpiredException"/> if no credentials are stored
    /// or if the refresh request fails.
    /// </summary>
    /// <param name="originalRequest">The request that received a 401 response.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The response from the retried request.</returns>
    /// <exception cref="AuthenticationExpiredException">
    /// Thrown when the session cannot be recovered. Caught by <see cref="ApiClient"/>.
    /// </exception>
    private async Task<HttpResponseMessage> HandleUnauthorizedAsync(
        HttpRequestMessage originalRequest,
        CancellationToken cancellationToken
    )
    {
        var credentials = await _credentialStore.GetAsync();

        if (credentials == null)
        {
            _logger.LogWarning(
                "Token refresh failed: no stored credentials available, session expired"
            );
            throw new AuthenticationExpiredException();
        }

        var tokenRequest = new HttpRequestMessage(
            HttpMethod.Post,
            new Uri(_baseAddress, "auth/token")
        )
        {
            Content = JsonContent.Create(
                new { email = credentials.Value.Email, password = credentials.Value.Password }
            ),
        };

        var tokenResponse = await base.SendAsync(tokenRequest, cancellationToken);

        if (!tokenResponse.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Token refresh failed with status {StatusCode}, session expired",
                tokenResponse.StatusCode
            );
            throw new AuthenticationExpiredException();
        }

        var token = await tokenResponse.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken);
        _tokenState.CurrentToken = token!.Token;

        var retryRequest = await CloneRequestAsync(originalRequest, cancellationToken);
        retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        retryRequest.Options.Set(IsRetryKey, true);

        return await base.SendAsync(retryRequest, cancellationToken);
    }

    /// <summary>
    /// Creates a deep copy of <paramref name="original"/>, preserving all headers
    /// and the request body so the request can be retried.
    /// </summary>
    /// <param name="original">The request to clone.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    private async Task<HttpRequestMessage> CloneRequestAsync(
        HttpRequestMessage original,
        CancellationToken cancellationToken
    )
    {
        var clone = new HttpRequestMessage(original.Method, original.RequestUri);

        foreach (var header in original.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        if (original.Content != null)
        {
            var bytes = await original.Content.ReadAsByteArrayAsync(cancellationToken);
            clone.Content = new ByteArrayContent(bytes);
            foreach (var header in original.Content.Headers)
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return clone;
    }

    private record TokenResponse(string Token, DateTime ExpiresAt, int UserId);
}
