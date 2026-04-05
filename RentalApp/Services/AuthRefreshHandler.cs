using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace RentalApp.Services;

public class AuthRefreshHandler : DelegatingHandler
{
    private static readonly HttpRequestOptionsKey<bool> IsRetryKey = new("IsRetry");

    private readonly AuthTokenState _tokenState;
    private readonly ICredentialStore _credentialStore;
    private readonly INavigationService _navigationService;
    private readonly Uri _baseAddress;

    public AuthRefreshHandler(
        AuthTokenState tokenState,
        ICredentialStore credentialStore,
        INavigationService navigationService,
        Uri baseAddress
    )
    {
        _tokenState = tokenState;
        _credentialStore = credentialStore;
        _navigationService = navigationService;
        _baseAddress = baseAddress;
    }

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
                response = await HandleUnauthorizedAsync(request, response, cancellationToken);
        }

        return response;
    }

    private async Task<HttpResponseMessage> HandleUnauthorizedAsync(
        HttpRequestMessage originalRequest,
        HttpResponseMessage unauthorizedResponse,
        CancellationToken cancellationToken
    )
    {
        var credentials = await _credentialStore.GetAsync();

        // If no stored credentials, return to login
        if (credentials == null)
        {
            await _navigationService.NavigateToRootAsync();
            return unauthorizedResponse;
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

        // If token refresh fails, return to login
        if (!tokenResponse.IsSuccessStatusCode)
        {
            await _navigationService.NavigateToRootAsync();
            return unauthorizedResponse;
        }

        var token = await tokenResponse.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken);
        _tokenState.CurrentToken = token!.Token;

        var retryRequest = await CloneRequestAsync(originalRequest, cancellationToken);
        retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        retryRequest.Options.Set(IsRetryKey, true);

        return await base.SendAsync(retryRequest, cancellationToken);
    }

    private async Task<HttpRequestMessage> CloneRequestAsync(
        HttpRequestMessage original,
        CancellationToken cancellationToken
    )
    {
        var clone = new HttpRequestMessage(original.Method, original.RequestUri);

        foreach (var header in original.Headers.Where(h => h.Key != "Authorization"))
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
