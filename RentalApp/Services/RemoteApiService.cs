using System.Net.Http.Json;
using RentalApp.Http;
using RentalApp.Models;

namespace RentalApp.Services;

/// <summary>
/// <see cref="IApiService"/> implementation that communicates with the remote HTTP API.
/// Bearer token authentication is handled internally via <see cref="AuthTokenState"/>.
/// </summary>
public class RemoteApiService : IApiService
{
    private readonly IApiClient _apiClient;
    private readonly AuthTokenState _tokenState;

    /// <summary>Initialises a new instance of <see cref="RemoteApiService"/>.</summary>
    /// <param name="apiClient">Typed HTTP client used to communicate with the remote API.</param>
    /// <param name="tokenState">Singleton bearer token holder shared across the HTTP layer.</param>
    public RemoteApiService(IApiClient apiClient, AuthTokenState tokenState)
    {
        _apiClient = apiClient;
        _tokenState = tokenState;
    }

    /// <inheritdoc/>
    /// <remarks>On success, the returned bearer token is stored in <see cref="AuthTokenState"/> and attached to all subsequent requests.</remarks>
    public async Task LoginAsync(string email, string password)
    {
        var response = await _apiClient.PostAsJsonAsync("auth/token", new { email, password });

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
            throw new UnauthorizedAccessException(error?.Message ?? "Login failed");
        }

        var token =
            await response.Content.ReadFromJsonAsync<AuthToken>()
            ?? throw new InvalidOperationException("Empty token response from API");

        _tokenState.CurrentToken = token.Token;
    }

    /// <inheritdoc/>
    public async Task RegisterAsync(
        string firstName,
        string lastName,
        string email,
        string password
    )
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
            throw new InvalidOperationException(error?.Message ?? "Registration failed");
        }
    }

    /// <inheritdoc/>
    /// <remarks>Maps the API's <c>MeResponse</c> DTO (which includes private fields such as email and account dates) to a <see cref="User"/>.</remarks>
    public async Task<User> GetCurrentUserAsync()
    {
        var response = await _apiClient.GetAsync("users/me");
        response.EnsureSuccessStatusCode();

        var dto =
            await response.Content.ReadFromJsonAsync<MeResponse>()
            ?? throw new InvalidOperationException("Empty profile response from API");

        return new User(
            dto.Id,
            dto.FirstName,
            dto.LastName,
            dto.AverageRating,
            dto.ItemsListed,
            dto.RentalsCompleted,
            dto.Email,
            dto.CreatedAt,
            Reviews: null
        );
    }

    /// <inheritdoc/>
    /// <remarks>Maps the API's <c>PublicProfileResponse</c> DTO (no email or account dates) to a <see cref="User"/>. Reviews are included when present.</remarks>
    public async Task<User> GetUserAsync(int userId)
    {
        var response = await _apiClient.GetAsync($"users/{userId}/profile");
        response.EnsureSuccessStatusCode();

        var dto =
            await response.Content.ReadFromJsonAsync<PublicProfileResponse>()
            ?? throw new InvalidOperationException("Empty profile response from API");

        return new User(
            dto.Id,
            dto.FirstName,
            dto.LastName,
            dto.AverageRating,
            dto.ItemsListed,
            dto.RentalsCompleted,
            Email: null,
            CreatedAt: null,
            dto.Reviews?.Select(r => new Review(
                    r.Id,
                    RentalId: null,
                    ItemId: null,
                    ReviewerId: null,
                    r.Rating,
                    ItemTitle: null,
                    r.Comment,
                    r.ReviewerName,
                    r.CreatedAt
                ))
                .ToList()
        );
    }

    /// <inheritdoc/>
    /// <remarks>Clears the bearer token from <see cref="AuthTokenState"/>. No network call is made.</remarks>
    public Task LogoutAsync()
    {
        _tokenState.CurrentToken = null;
        return Task.CompletedTask;
    }

    public Task<List<Item>> GetItemsAsync(
        string? category = null,
        string? search = null,
        int page = 1
    ) => throw new NotImplementedException();

    public Task<List<Item>> GetNearbyItemsAsync(
        double lat,
        double lon,
        double radius = 5.0,
        string? category = null
    ) => throw new NotImplementedException();

    public Task<Item> GetItemAsync(int id) => throw new NotImplementedException();

    public Task<Item> CreateItemAsync(
        string title,
        string? description,
        double dailyRate,
        int categoryId,
        double latitude,
        double longitude
    ) => throw new NotImplementedException();

    public Task<Item> UpdateItemAsync(
        int id,
        string? title,
        string? description,
        double? dailyRate,
        bool? isAvailable
    ) => throw new NotImplementedException();

    public Task<List<Category>> GetCategoriesAsync() => throw new NotImplementedException();

    private sealed record MeResponse(
        int Id,
        string Email,
        string FirstName,
        string LastName,
        double? AverageRating,
        int ItemsListed,
        int RentalsCompleted,
        DateTime CreatedAt
    );

    private sealed record PublicProfileResponse(
        int Id,
        string FirstName,
        string LastName,
        double? AverageRating,
        int ItemsListed,
        int RentalsCompleted,
        List<ReviewResponse>? Reviews
    );

    private sealed record ReviewResponse(
        int Id,
        int Rating,
        string? Comment,
        string ReviewerName,
        DateTime CreatedAt
    );

    private sealed record ApiErrorResponse(string Error, string Message);

    private sealed record AuthToken(string Token, DateTime ExpiresAt, int UserId);
}
