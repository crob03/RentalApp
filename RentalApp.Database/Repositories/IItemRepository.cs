using NetTopologySuite.Geometries;
using DbItem = RentalApp.Database.Models.Item;

namespace RentalApp.Database.Repositories;

/// <summary>
/// Pairs a fully-hydrated <see cref="DbItem"/> with the geodesic distance (in metres) from the
/// search origin, as computed by PostGIS <c>ST_Distance</c> on the <c>geography</c> column.
/// </summary>
/// <param name="Item">The item entity, including its <c>Category</c> and <c>Owner</c> navigation properties.</param>
/// <param name="DistanceMeters">Geodesic distance from the search origin to <paramref name="Item"/>'s location, in metres.</param>
public record NearbyItemResult(DbItem Item, double DistanceMeters);

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
    /// ordered by ascending distance from the origin. Each result includes the geodesic distance in metres
    /// as computed by PostGIS — callers must not recompute distance from the NTS geometry, which would give
    /// degrees, not metres.
    /// </summary>
    /// <param name="origin">Geographic origin point (SRID 4326).</param>
    /// <param name="radiusMeters">Search radius in metres, passed directly to PostGIS <c>ST_DWithin</c>.</param>
    /// <param name="category">Category slug to filter by; <see langword="null"/> returns all categories.</param>
    Task<IEnumerable<NearbyItemResult>> GetNearbyItemsAsync(
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
    /// Returns a map of category ID to item count for all categories that have at least one item.
    /// Categories with no items are absent from the result; callers should use
    /// <see cref="Dictionary{TKey,TValue}.GetValueOrDefault(TKey)"/> with a default of 0.
    /// </summary>
    Task<Dictionary<int, int>> CountItemsByCategoryAsync();

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
