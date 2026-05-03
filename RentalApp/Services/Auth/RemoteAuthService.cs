using System.Net.Http.Json;
using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;
using RentalApp.Http;
using RentalApp.Services;

namespace RentalApp.Services.Auth;

/// <summary>
/// HTTP implementation of <see cref="IAuthService"/> that delegates all operations to the remote API via <see cref="IApiClient"/>.
/// </summary>
internal class RemoteAuthService : RemoteServiceBase, IAuthService
{
    private readonly IApiClient _apiClient;
    private CurrentUserResponse? _currentUserCache;

    public RemoteAuthService(IApiClient apiClient) => _apiClient = apiClient;

    /// <inheritdoc/>
    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        _currentUserCache = null;
        var response = await _apiClient.PostAsJsonAsync(
            "auth/token",
            new { email = request.Email, password = request.Password }
        );
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<LoginResponse>()
            ?? throw new InvalidOperationException("Empty token response from API");
    }

    /// <inheritdoc/>
    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
    {
        var response = await _apiClient.PostAsJsonAsync(
            "auth/register",
            new
            {
                firstName = request.FirstName,
                lastName = request.LastName,
                email = request.Email,
                password = request.Password,
            }
        );
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<RegisterResponse>()
            ?? throw new InvalidOperationException("Empty register response from API");
    }

    /// <inheritdoc/>
    public async Task<CurrentUserResponse> GetCurrentUserAsync()
    {
        if (_currentUserCache is not null)
            return _currentUserCache;
        var response = await _apiClient.GetAsync("users/me");
        await EnsureSuccessAsync(response);
        _currentUserCache =
            await response.Content.ReadFromJsonAsync<CurrentUserResponse>()
            ?? throw new InvalidOperationException("Empty profile response from API");
        return _currentUserCache;
    }

    /// <inheritdoc/>
    public async Task<UserProfileResponse> GetUserProfileAsync(int userId)
    {
        var response = await _apiClient.GetAsync($"users/{userId}/profile");
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<UserProfileResponse>()
            ?? throw new InvalidOperationException("Empty profile response from API");
    }
}
