using System.Net.Http.Json;
using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;
using RentalApp.Http;
using RentalApp.Services;
using static System.FormattableString;

namespace RentalApp.Services.Items;

/// <summary>
/// HTTP implementation of <see cref="IItemService"/> that delegates all operations to the remote API via <see cref="IApiClient"/>.
/// </summary>
internal class RemoteItemService : RemoteServiceBase, IItemService
{
    private readonly IApiClient _apiClient;

    public RemoteItemService(IApiClient apiClient) => _apiClient = apiClient;

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public async Task<ItemDetailResponse> GetItemAsync(int id)
    {
        var response = await _apiClient.GetAsync($"items/{id}");
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<ItemDetailResponse>()
            ?? throw new InvalidOperationException("Empty item response from API");
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public async Task<CategoriesResponse> GetCategoriesAsync()
    {
        var response = await _apiClient.GetAsync("categories");
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<CategoriesResponse>()
            ?? throw new InvalidOperationException("Empty categories response from API");
    }
}
