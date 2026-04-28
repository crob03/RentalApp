using RentalApp.Models;

namespace RentalApp.Services;

public class ItemService : IItemService
{
    private readonly IApiService _api;

    public ItemService(IApiService api)
    {
        _api = api;
    }

    public Task<List<Item>> GetItemsAsync(
        string? category = null,
        string? search = null,
        int page = 1,
        int pageSize = 20
    ) => _api.GetItemsAsync(category, search, page, pageSize);

    public Task<List<Item>> GetNearbyItemsAsync(
        double lat,
        double lon,
        double radius = 5.0,
        string? category = null,
        int page = 1,
        int pageSize = 20
    ) => _api.GetNearbyItemsAsync(lat, lon, radius, category, page, pageSize);

    public Task<Item> GetItemAsync(int id) => _api.GetItemAsync(id);

    public async Task<Item> CreateItemAsync(
        string title,
        string? description,
        double dailyRate,
        int categoryId,
        double lat,
        double lon
    )
    {
        ValidateTitle(title);
        ValidateDescription(description);
        ValidateDailyRate(dailyRate);
        ValidateCategoryId(categoryId);

        return await _api.CreateItemAsync(title, description, dailyRate, categoryId, lat, lon);
    }

    public async Task<Item> UpdateItemAsync(
        int id,
        string? title,
        string? description,
        double? dailyRate,
        bool? isAvailable
    )
    {
        if (title != null)
            ValidateTitle(title);
        if (description != null)
            ValidateDescription(description);
        if (dailyRate.HasValue)
            ValidateDailyRate(dailyRate.Value);

        return await _api.UpdateItemAsync(id, title, description, dailyRate, isAvailable);
    }

    public Task<List<Category>> GetCategoriesAsync() => _api.GetCategoriesAsync();

    private static void ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Length < 5 || title.Length > 100)
            throw new ArgumentException(
                "Title must be between 5 and 100 characters.",
                nameof(title)
            );
    }

    private static void ValidateDescription(string? description)
    {
        if (description != null && description.Length > 1000)
            throw new ArgumentException(
                "Description must not exceed 1000 characters.",
                nameof(description)
            );
    }

    private static void ValidateDailyRate(double dailyRate)
    {
        if (dailyRate <= 0 || dailyRate > 1000)
            throw new ArgumentException(
                "Daily rate must be greater than 0 and at most 1000.",
                nameof(dailyRate)
            );
    }

    private static void ValidateCategoryId(int categoryId)
    {
        if (categoryId <= 0)
            throw new ArgumentException("Category ID must be positive.", nameof(categoryId));
    }
}
