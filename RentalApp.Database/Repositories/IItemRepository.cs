using NetTopologySuite.Geometries;
using DbItem = RentalApp.Database.Models.Item;

namespace RentalApp.Database.Repositories;

/// <summary>
/// Data-access contract for item queries and mutations.
/// </summary>
public interface IItemRepository
{
    /// <summary>
    /// Returns a paginated, optionally filtered list of items ordered by creation date.
    /// </summary>
    /// <param name="category">Category slug to filter by; <see langword="null"/> returns all categories.</param>
    /// <param name="search">Case-insensitive title substring match; <see langword="null"/> skips text filtering.</param>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Maximum records per page.</param>
    Task<IEnumerable<DbItem>> GetItemsAsync(
        string? category,
        string? search,
        int page,
        int pageSize
    );

    /// <summary>
    /// Returns all items whose location falls within <paramref name="radiusMeters"/> of <paramref name="origin"/>,
    /// ordered by ascending distance from the origin.
    /// </summary>
    /// <param name="origin">Geographic origin point (SRID 4326).</param>
    /// <param name="radiusMeters">Search radius in metres, passed directly to PostGIS <c>ST_DWithin</c>.</param>
    /// <param name="category">Category slug to filter by; <see langword="null"/> returns all categories.</param>
    Task<IEnumerable<DbItem>> GetNearbyItemsAsync(
        Point origin,
        double radiusMeters,
        string? category
    );

    /// <summary>
    /// Returns the total number of items matching the given filters, for use in pagination metadata.
    /// </summary>
    /// <param name="category">Category slug to filter by; <see langword="null"/> returns all categories.</param>
    /// <param name="search">Case-insensitive title substring match; <see langword="null"/> skips text filtering.</param>
    Task<int> CountItemsAsync(string? category, string? search);

    /// <summary>Returns the item with the given <paramref name="id"/>, including its category and owner, or <see langword="null"/> if not found.</summary>
    Task<DbItem?> GetItemAsync(int id);

    /// <summary>
    /// Persists a new item listing and returns the fully-hydrated entity (with navigation properties).
    /// </summary>
    Task<DbItem> CreateItemAsync(
        string title,
        string? description,
        double dailyRate,
        int categoryId,
        int ownerId,
        Point location
    );

    /// <summary>Returns the number of items owned by the user with the given <paramref name="ownerId"/>.</summary>
    Task<int> CountItemsByOwnerAsync(int ownerId);

    /// <summary>
    /// Applies a partial update to an existing item; <see langword="null"/> parameters are left unchanged.
    /// Passing an empty string for <paramref name="description"/> clears the field.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if no item with <paramref name="id"/> exists.</exception>
    Task<DbItem> UpdateItemAsync(
        int id,
        string? title,
        string? description,
        double? dailyRate,
        bool? isAvailable
    );
}
