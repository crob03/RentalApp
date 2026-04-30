using RentalApp.Models;

namespace RentalApp.Services;

/// <summary>
/// Application-layer service for item operations. Validates inputs before delegating to the underlying API.
/// </summary>
public interface IItemService
{
    /// <summary>Returns a paginated list of items, optionally filtered by category slug or search text.</summary>
    Task<List<Item>> GetItemsAsync(
        string? category = null,
        string? search = null,
        int page = 1,
        int pageSize = 20
    );

    /// <summary>Returns items within <paramref name="radius"/> kilometres of the supplied coordinates, ordered by distance.</summary>
    /// <param name="lat">Latitude of the search origin.</param>
    /// <param name="lon">Longitude of the search origin.</param>
    /// <param name="radius">Search radius in kilometres (default 5).</param>
    /// <param name="category">Category slug to filter by.</param>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Maximum records per page.</param>
    Task<List<Item>> GetNearbyItemsAsync(
        double lat,
        double lon,
        double radius = 5.0,
        string? category = null,
        int page = 1,
        int pageSize = 20
    );

    /// <summary>Returns the item with the given <paramref name="id"/>.</summary>
    Task<Item> GetItemAsync(int id);

    /// <summary>
    /// Validates and creates a new item listing. The item location is captured from the
    /// supplied coordinates at the time of creation.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when title, description, dailyRate, or categoryId fail validation.</exception>
    Task<Item> CreateItemAsync(
        string title,
        string? description,
        double dailyRate,
        int categoryId,
        double lat,
        double lon
    );

    /// <summary>
    /// Validates and partially updates an item. Only non-<see langword="null"/> arguments are applied.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when any provided value fails validation.</exception>
    Task<Item> UpdateItemAsync(
        int id,
        string? title,
        string? description,
        double? dailyRate,
        bool? isAvailable
    );

    /// <summary>Returns all available item categories.</summary>
    Task<List<Category>> GetCategoriesAsync();
}
