using System.Net.Http.Json;
using RentalApp.Models;

namespace RentalApp.Services;

public class RemoteApiService : IApiService
{
    private readonly IApiClient _apiClient;
    private readonly AuthTokenState _tokenState;

    public RemoteApiService(IApiClient apiClient, AuthTokenState tokenState)
    {
        _apiClient = apiClient;
        _tokenState = tokenState;
    }

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

    public async Task<User> GetUserAsync(int userId)
    {
        var response = await _apiClient.GetAsync($"users/{userId}");
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

    public Task LogoutAsync()
    {
        _tokenState.CurrentToken = null;
        return Task.CompletedTask;
    }

    // ── Future domain methods ──────────────────────────────────────
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

    public Task<Rental> RequestRentalAsync(int itemId, DateOnly startDate, DateOnly endDate) =>
        throw new NotImplementedException();

    public Task<List<Rental>> GetIncomingRentalsAsync(string? status = null) =>
        throw new NotImplementedException();

    public Task<List<Rental>> GetOutgoingRentalsAsync(string? status = null) =>
        throw new NotImplementedException();

    public Task<Rental> GetRentalAsync(int id) => throw new NotImplementedException();

    public Task UpdateRentalStatusAsync(int rentalId, string status) =>
        throw new NotImplementedException();

    public Task<Review> CreateReviewAsync(int rentalId, int rating, string comment) =>
        throw new NotImplementedException();

    public Task<List<Review>> GetItemReviewsAsync(int itemId, int page = 1) =>
        throw new NotImplementedException();

    public Task<List<Review>> GetUserReviewsAsync(int userId, int page = 1) =>
        throw new NotImplementedException();

    private record MeResponse(
        int Id,
        string Email,
        string FirstName,
        string LastName,
        double? AverageRating,
        int ItemsListed,
        int RentalsCompleted,
        DateTime CreatedAt
    );

    private record PublicProfileResponse(
        int Id,
        string FirstName,
        string LastName,
        double? AverageRating,
        int ItemsListed,
        int RentalsCompleted,
        List<ReviewResponse>? Reviews
    );

    private record ReviewResponse(
        int Id,
        int Rating,
        string? Comment,
        string ReviewerName,
        DateTime CreatedAt
    );

    private record ApiErrorResponse(string Error, string Message);
}
