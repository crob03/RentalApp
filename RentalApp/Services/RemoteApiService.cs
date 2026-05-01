// RentalApp/Services/RemoteApiService.cs
using System.Net.Http.Json;
using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;
using RentalApp.Http;
using static System.FormattableString;

namespace RentalApp.Services;

public class RemoteApiService : IApiService
{
    private readonly IApiClient _apiClient;

    public RemoteApiService(IApiClient apiClient) => _apiClient = apiClient;

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var response = await _apiClient.PostAsJsonAsync(
            "auth/token",
            new { email = request.Email, password = request.Password }
        );
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<LoginResponse>()
            ?? throw new InvalidOperationException("Empty token response from API");
    }

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

    public async Task<CurrentUserResponse> GetCurrentUserAsync()
    {
        var response = await _apiClient.GetAsync("users/me");
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<CurrentUserResponse>()
            ?? throw new InvalidOperationException("Empty profile response from API");
    }

    public async Task<UserProfileResponse> GetUserProfileAsync(int userId)
    {
        var response = await _apiClient.GetAsync($"users/{userId}/profile");
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<UserProfileResponse>()
            ?? throw new InvalidOperationException("Empty profile response from API");
    }

    public async Task<ItemsResponse> GetItemsAsync(GetItemsRequest request)
    {
        var query = Invariant($"items?page={request.Page}&pageSize={request.PageSize}");
        if (request.Category != null)
            query += $"&category={Uri.EscapeDataString(request.Category)}";
        if (!string.IsNullOrEmpty(request.Search))
            query += $"&search={Uri.EscapeDataString(request.Search)}";

        var response = await _apiClient.GetAsync(query);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<ItemsResponse>()
            ?? throw new InvalidOperationException("Empty items response from API");
    }

    public async Task<NearbyItemsResponse> GetNearbyItemsAsync(GetNearbyItemsRequest request)
    {
        var query = Invariant(
            $"items/nearby?lat={request.Lat}&lon={request.Lon}&radius={request.Radius}"
        );
        if (request.Category != null)
            query += $"&category={Uri.EscapeDataString(request.Category)}";

        var response = await _apiClient.GetAsync(query);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<NearbyItemsResponse>()
            ?? throw new InvalidOperationException("Empty nearby items response from API");
    }

    public async Task<ItemDetailResponse> GetItemAsync(int id)
    {
        var response = await _apiClient.GetAsync($"items/{id}");
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<ItemDetailResponse>()
            ?? throw new InvalidOperationException("Empty item response from API");
    }

    public async Task<CreateItemResponse> CreateItemAsync(CreateItemRequest request)
    {
        var response = await _apiClient.PostAsJsonAsync(
            "items",
            new
            {
                title = request.Title,
                description = request.Description,
                dailyRate = request.DailyRate,
                categoryId = request.CategoryId,
                latitude = request.Latitude,
                longitude = request.Longitude,
            }
        );
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<CreateItemResponse>()
            ?? throw new InvalidOperationException("Empty create item response from API");
    }

    public async Task<UpdateItemResponse> UpdateItemAsync(int id, UpdateItemRequest request)
    {
        var response = await _apiClient.PutAsJsonAsync(
            $"items/{id}",
            new
            {
                title = request.Title,
                description = request.Description,
                dailyRate = request.DailyRate,
                isAvailable = request.IsAvailable,
            }
        );
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<UpdateItemResponse>()
            ?? throw new InvalidOperationException("Empty update item response from API");
    }

    public async Task<CategoriesResponse> GetCategoriesAsync()
    {
        var response = await _apiClient.GetAsync("categories");
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<CategoriesResponse>()
            ?? throw new InvalidOperationException("Empty categories response from API");
    }

    public Task<RentalsListResponse> GetIncomingRentalsAsync(GetRentalsRequest request) =>
        GetRentalsAsync("rentals/incoming", request.Status);

    public Task<RentalsListResponse> GetOutgoingRentalsAsync(GetRentalsRequest request) =>
        GetRentalsAsync("rentals/outgoing", request.Status);

    public async Task<RentalDetailResponse> GetRentalAsync(int id)
    {
        var response = await _apiClient.GetAsync($"rentals/{id}");
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<RentalDetailResponse>()
            ?? throw new InvalidOperationException("Empty rental response from API");
    }

    public async Task<RentalSummaryResponse> CreateRentalAsync(CreateRentalRequest request)
    {
        var response = await _apiClient.PostAsJsonAsync(
            "rentals",
            new
            {
                itemId = request.ItemId,
                startDate = request.StartDate.ToString(
                    "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture
                ),
                endDate = request.EndDate.ToString(
                    "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture
                ),
            }
        );
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<RentalSummaryResponse>()
            ?? throw new InvalidOperationException("Empty create rental response from API");
    }

    public async Task<UpdateRentalStatusResponse> UpdateRentalStatusAsync(
        int id,
        UpdateRentalStatusRequest request
    )
    {
        var response = await _apiClient.PutAsJsonAsync(
            $"rentals/{id}/status",
            new { status = request.Status }
        );
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<UpdateRentalStatusResponse>()
            ?? throw new InvalidOperationException("Empty update status response from API");
    }

    public async Task<ReviewsResponse> GetItemReviewsAsync(int itemId, GetReviewsRequest request)
    {
        var response = await _apiClient.GetAsync(
            Invariant($"items/{itemId}/reviews?page={request.Page}&pageSize={request.PageSize}")
        );
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<ReviewsResponse>()
            ?? throw new InvalidOperationException("Empty reviews response from API");
    }

    public async Task<ReviewsResponse> GetUserReviewsAsync(int userId, GetReviewsRequest request)
    {
        var response = await _apiClient.GetAsync(
            Invariant($"users/{userId}/reviews?page={request.Page}&pageSize={request.PageSize}")
        );
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<ReviewsResponse>()
            ?? throw new InvalidOperationException("Empty reviews response from API");
    }

    public async Task<CreateReviewResponse> CreateReviewAsync(CreateReviewRequest request)
    {
        var response = await _apiClient.PostAsJsonAsync(
            "reviews",
            new
            {
                rentalId = request.RentalId,
                rating = request.Rating,
                comment = request.Comment,
            }
        );
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<CreateReviewResponse>()
            ?? throw new InvalidOperationException("Empty create review response from API");
    }

    private async Task<RentalsListResponse> GetRentalsAsync(string path, string? status)
    {
        var query = status != null ? $"{path}?status={Uri.EscapeDataString(status)}" : path;
        var response = await _apiClient.GetAsync(query);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<RentalsListResponse>()
            ?? throw new InvalidOperationException("Empty rentals response from API");
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        throw new HttpRequestException(
            error?.Message ?? $"Request failed with status {(int)response.StatusCode}"
        );
    }

    private sealed record ApiErrorResponse(string Error, string Message);
}
