using System.Net.Http.Headers;
using System.Net.Http.Json;
using RentalApp.Database.Models;

namespace RentalApp.Services;

public class ApiAuthenticationService : IAuthenticationService
{
    private readonly HttpClient _httpClient;
    private readonly AuthTokenState _tokenState;
    private readonly ICredentialStore _credentialStore;
    private User? _currentUser;

    public event EventHandler<bool>? AuthenticationStateChanged;

    public bool IsAuthenticated => _currentUser != null;
    public User? CurrentUser => _currentUser;

    public ApiAuthenticationService(
        HttpClient httpClient,
        AuthTokenState tokenState,
        ICredentialStore credentialStore
    )
    {
        _httpClient = httpClient;
        _tokenState = tokenState;
        _credentialStore = credentialStore;
    }

    public async Task<AuthenticationResult> LoginAsync(
        string email,
        string password,
        bool rememberMe = false
    )
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("auth/token", new { email, password });

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
                return AuthenticationResult.Failure(error?.Message ?? "Login failed");
            }

            var token = await response.Content.ReadFromJsonAsync<TokenResponse>();
            _tokenState.CurrentToken = token!.Token;

            if (rememberMe)
                await _credentialStore.SaveAsync(email, password);

            var meResponse = await _httpClient.GetAsync("users/me");
            if (!meResponse.IsSuccessStatusCode)
                return AuthenticationResult.Failure("Failed to retrieve user profile");

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
            return AuthenticationResult.Success(_currentUser);
        }
        catch (Exception ex)
        {
            return AuthenticationResult.Failure($"Login failed: {ex.Message}");
        }
    }

    public async Task<AuthenticationResult> RegisterAsync(
        string firstName,
        string lastName,
        string email,
        string password
    )
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
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
                return AuthenticationResult.Failure(error?.Message ?? "Registration failed");
            }

            return AuthenticationResult.Success();
        }
        catch (Exception ex)
        {
            return AuthenticationResult.Failure($"Registration failed: {ex.Message}");
        }
    }

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
