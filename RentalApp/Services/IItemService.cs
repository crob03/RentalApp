using RentalApp.Models;

namespace RentalApp.Services;

public interface IItemService
{
    Task<List<Item>> GetItemsAsync(
        string? category = null,
        string? search = null,
        int page = 1,
        int pageSize = 20
    );

    Task<List<Item>> GetNearbyItemsAsync(
        double lat,
        double lon,
        double radius = 5.0,
        string? category = null,
        int page = 1,
        int pageSize = 20
    );

    Task<Item> GetItemAsync(int id);

    Task<Item> CreateItemAsync(
        string title,
        string? description,
        double dailyRate,
        int categoryId,
        double lat,
        double lon
    );

    Task<Item> UpdateItemAsync(
        int id,
        string? title,
        string? description,
        double? dailyRate,
        bool? isAvailable
    );

    Task<List<Category>> GetCategoriesAsync();
}
