using System.Net.Http.Json;
using RentalApp.Http;
using RentalApp.Models;
using static System.FormattableString;

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

    /// <inheritdoc/>
    /// <remarks>Category and search terms are URL-encoded before appending to the query string. The list DTO omits coordinates; <c>Latitude</c>/<c>Longitude</c> are <see langword="null"/> on returned items.</remarks>
    public async Task<List<Item>> GetItemsAsync(
        string? category = null,
        string? search = null,
        int page = 1,
        int pageSize = 20
    )
    {
        var query = $"items?page={page}&pageSize={pageSize}";
        if (category != null)
            query += $"&category={Uri.EscapeDataString(category)}";
        if (!string.IsNullOrEmpty(search))
            query += $"&search={Uri.EscapeDataString(search)}";

        var response = await _apiClient.GetAsync(query);
        response.EnsureSuccessStatusCode();

        var dto =
            await response.Content.ReadFromJsonAsync<ItemsListResponse>()
            ?? throw new InvalidOperationException("Empty items response from API");

        return dto
            .Items.Select(i => new Item(
                i.Id,
                i.Title,
                i.Description,
                i.DailyRate,
                i.CategoryId,
                i.Category,
                i.OwnerId,
                i.OwnerName,
                i.OwnerRating,
                Latitude: null,
                Longitude: null,
                Distance: null,
                i.IsAvailable,
                i.AverageRating,
                TotalReviews: null,
                i.CreatedAt,
                Reviews: null
            ))
            .ToList();
    }

    /// <inheritdoc/>
    /// <remarks><c>FormattableString.Invariant</c> is used to format the URL so that decimal values use <c>.</c> as the separator regardless of the current thread culture.</remarks>
    public async Task<List<Item>> GetNearbyItemsAsync(
        double lat,
        double lon,
        double radius = 5.0,
        string? category = null,
        int page = 1,
        int pageSize = 20
    )
    {
        var query = Invariant(
            $"items/nearby?lat={lat}&lon={lon}&radius={radius}&page={page}&pageSize={pageSize}"
        );
        if (category != null)
            query += $"&category={Uri.EscapeDataString(category)}";

        var response = await _apiClient.GetAsync(query);
        response.EnsureSuccessStatusCode();

        var dto =
            await response.Content.ReadFromJsonAsync<NearbyItemsResponse>()
            ?? throw new InvalidOperationException("Empty nearby items response from API");

        return dto
            .Items.Select(i => new Item(
                i.Id,
                i.Title,
                i.Description,
                i.DailyRate,
                i.CategoryId,
                i.Category,
                i.OwnerId,
                i.OwnerName,
                OwnerRating: null,
                i.Latitude,
                i.Longitude,
                i.Distance,
                i.IsAvailable,
                i.AverageRating,
                TotalReviews: null,
                CreatedAt: null,
                Reviews: null
            ))
            .ToList();
    }

    /// <inheritdoc/>
    /// <remarks>Returns the full detail DTO including reviews and owner rating, unlike the list endpoints which return summary data only.</remarks>
    public async Task<Item> GetItemAsync(int id)
    {
        var response = await _apiClient.GetAsync($"items/{id}");
        response.EnsureSuccessStatusCode();

        var dto =
            await response.Content.ReadFromJsonAsync<ItemDetailDto>()
            ?? throw new InvalidOperationException("Empty item response from API");

        return new Item(
            dto.Id,
            dto.Title,
            dto.Description,
            dto.DailyRate,
            dto.CategoryId,
            dto.Category,
            dto.OwnerId,
            dto.OwnerName,
            dto.OwnerRating,
            dto.Latitude,
            dto.Longitude,
            Distance: null,
            dto.IsAvailable,
            dto.AverageRating,
            dto.TotalReviews,
            dto.CreatedAt,
            dto.Reviews.Select(r => new Review(
                    r.Id,
                    RentalId: null,
                    ItemId: dto.Id,
                    r.ReviewerId,
                    r.Rating,
                    ItemTitle: dto.Title,
                    r.Comment,
                    r.ReviewerName,
                    r.CreatedAt
                ))
                .ToList()
        );
    }

    /// <inheritdoc/>
    public async Task<Item> CreateItemAsync(
        string title,
        string? description,
        double dailyRate,
        int categoryId,
        double latitude,
        double longitude
    )
    {
        var response = await _apiClient.PostAsJsonAsync(
            "items",
            new
            {
                title,
                description,
                dailyRate,
                categoryId,
                latitude,
                longitude,
            }
        );
        response.EnsureSuccessStatusCode();

        var dto =
            await response.Content.ReadFromJsonAsync<ItemCreateResponse>()
            ?? throw new InvalidOperationException("Empty create item response from API");

        return new Item(
            dto.Id,
            dto.Title,
            dto.Description,
            dto.DailyRate,
            dto.CategoryId,
            dto.Category,
            dto.OwnerId,
            dto.OwnerName,
            OwnerRating: null,
            dto.Latitude,
            dto.Longitude,
            Distance: null,
            dto.IsAvailable,
            AverageRating: null,
            TotalReviews: null,
            dto.CreatedAt,
            Reviews: null
        );
    }

    /// <inheritdoc/>
    /// <remarks>The API returns a minimal acknowledgement; the method re-fetches via <see cref="GetItemAsync"/> to return a fully-hydrated item.</remarks>
    public async Task<Item> UpdateItemAsync(
        int id,
        string? title,
        string? description,
        double? dailyRate,
        bool? isAvailable
    )
    {
        var response = await _apiClient.PutAsJsonAsync(
            $"items/{id}",
            new
            {
                title,
                description,
                dailyRate,
                isAvailable,
            }
        );
        response.EnsureSuccessStatusCode();

        return await GetItemAsync(id);
    }

    /// <inheritdoc/>
    public async Task<List<Category>> GetCategoriesAsync()
    {
        var response = await _apiClient.GetAsync("categories");
        response.EnsureSuccessStatusCode();

        var dto =
            await response.Content.ReadFromJsonAsync<CategoriesResponse>()
            ?? throw new InvalidOperationException("Empty categories response from API");

        return dto.Categories.Select(c => new Category(c.Id, c.Name, c.Slug, c.ItemCount)).ToList();
    }

    private sealed record ItemsListResponse(
        List<ItemListDto> Items,
        int TotalItems,
        int Page,
        int PageSize,
        int TotalPages
    );

    private sealed record ItemListDto(
        int Id,
        string Title,
        string? Description,
        double DailyRate,
        int CategoryId,
        string Category,
        int OwnerId,
        string OwnerName,
        double? OwnerRating,
        bool IsAvailable,
        double? AverageRating,
        DateTime CreatedAt
    );

    private sealed record NearbyItemsResponse(List<NearbyItemDto> Items, int TotalResults);

    private sealed record NearbyItemDto(
        int Id,
        string Title,
        string? Description,
        double DailyRate,
        int CategoryId,
        string Category,
        int OwnerId,
        string OwnerName,
        double Latitude,
        double Longitude,
        double Distance,
        bool IsAvailable,
        double? AverageRating
    );

    private sealed record ItemDetailDto(
        int Id,
        string Title,
        string? Description,
        double DailyRate,
        int CategoryId,
        string Category,
        int OwnerId,
        string OwnerName,
        double? OwnerRating,
        double? Latitude,
        double? Longitude,
        bool IsAvailable,
        double? AverageRating,
        int TotalReviews,
        DateTime CreatedAt,
        List<ItemReviewDto> Reviews
    );

    private sealed record ItemReviewDto(
        int Id,
        int ReviewerId,
        string ReviewerName,
        int Rating,
        string? Comment,
        DateTime CreatedAt
    );

    private sealed record ItemCreateResponse(
        int Id,
        string Title,
        string? Description,
        double DailyRate,
        int CategoryId,
        string Category,
        int OwnerId,
        string OwnerName,
        double Latitude,
        double Longitude,
        bool IsAvailable,
        DateTime CreatedAt
    );

    private sealed record CategoriesResponse(List<CategoryDto> Categories);

    private sealed record CategoryDto(int Id, string Name, string Slug, int ItemCount);

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
