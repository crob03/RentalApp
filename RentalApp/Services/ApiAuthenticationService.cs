using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using RentalApp.Database.Models;

namespace RentalApp.Services;

/// <summary>
/// Implements <see cref="IAuthenticationService"/> by communicating with a remote REST API.
/// Tokens are stored in <see cref="AuthTokenState"/> and attached to outgoing requests by
/// <see cref="AuthRefreshHandler"/>.
/// </summary>
public class ApiAuthenticationService : IAuthenticationService
{
    private readonly IApiClient _apiClient;
    private readonly AuthTokenState _tokenState;
    private readonly ICredentialStore _credentialStore;
    private readonly ILogger<ApiAuthenticationService> _logger;
    private User? _currentUser;

    /// <inheritdoc/>
    public event EventHandler<bool>? AuthenticationStateChanged;

    /// <inheritdoc/>
    public bool IsAuthenticated => _currentUser != null;

    /// <inheritdoc/>
    public User? CurrentUser => _currentUser;

    /// <summary>
    /// Initialises a new instance of <see cref="ApiAuthenticationService"/>.
    /// </summary>
    /// <param name="apiClient">The API client used to communicate with the API.</param>
    /// <param name="tokenState">The shared token state updated on login and cleared on logout.</param>
    /// <param name="credentialStore">The credential store used to persist credentials when remember-me is enabled.</param>
    /// <param name="logger">The logger for this service.</param>
    public ApiAuthenticationService(
        IApiClient apiClient,
        AuthTokenState tokenState,
        ICredentialStore credentialStore,
        ILogger<ApiAuthenticationService> logger
    )
    {
        _apiClient = apiClient;
        _tokenState = tokenState;
        _credentialStore = credentialStore;
        _logger = logger;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Posts credentials to <c>auth/token</c>, stores the returned bearer token, then fetches
    /// the user profile from <c>users/me</c> to populate <see cref="CurrentUser"/>.
    /// </remarks>
    public async Task<AuthenticationResult> LoginAsync(
        string email,
        string password,
        bool rememberMe = false
    )
    {
        try
        {
            var response = await _apiClient.PostAsJsonAsync("auth/token", new { email, password });

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
                var message = error?.Message ?? "Login failed";
                _logger.LogWarning("Login failed for {Email}: {Message}", email, message);
                return AuthenticationResult.Failure(message);
            }

            var token = await response.Content.ReadFromJsonAsync<TokenResponse>();
            _tokenState.CurrentToken = token!.Token;

            if (rememberMe)
                await _credentialStore.SaveAsync(email, password);

            var meResponse = await _apiClient.GetAsync("users/me");
            if (!meResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to retrieve user profile after login for {Email} (status {StatusCode})",
                    email,
                    meResponse.StatusCode
                );
                return AuthenticationResult.Failure("Failed to retrieve user profile");
            }

            var profile = await meResponse.Content.ReadFromJsonAsync<UserProfileResponse>();

            _currentUser = new User
            {
                Id = profile!.Id,
                Email = profile.Email,
                FirstName = profile.FirstName,
                LastName = profile.LastName,
                CreatedAt = profile.CreatedAt,
            };

            AuthenticationStateChanged?.Invoke(this, true);
            return AuthenticationResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during login for {Email}", email);
            return AuthenticationResult.Failure($"Login failed: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Posts the registration payload to <c>auth/register</c>. The user must log in separately
    /// after a successful registration.
    /// </remarks>
    public async Task<AuthenticationResult> RegisterAsync(
        string firstName,
        string lastName,
        string email,
        string password
    )
    {
        try
        {
            var response = await _apiClient.PostAsJsonAsync(
                "auth/register",
                new
                {
                    firstName,
                    lastName,
                    email,
                    password,
                }
            );

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
                var message = error?.Message ?? "Registration failed";
                _logger.LogWarning("Registration failed for {Email}: {Message}", email, message);
                return AuthenticationResult.Failure(message);
            }

            return AuthenticationResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during registration for {Email}", email);
            return AuthenticationResult.Failure($"Registration failed: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task LogoutAsync()
    {
        _currentUser = null;
        _tokenState.CurrentToken = null;
        await _credentialStore.ClearAsync();
        AuthenticationStateChanged?.Invoke(this, false);
    }

    private record TokenResponse(string Token, DateTime ExpiresAt, int UserId);

    private record UserProfileResponse(
        int Id,
        string Email,
        string FirstName,
        string LastName,
        DateTime CreatedAt
    );

    private record ApiErrorResponse(string Error, string Message);
}
