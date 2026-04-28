using NetTopologySuite.Geometries;
using DbItem = RentalApp.Database.Models.Item;

namespace RentalApp.Database.Repositories;

public interface IItemRepository
{
    Task<IEnumerable<DbItem>> GetItemsAsync(
        string? category,
        string? search,
        int page,
        int pageSize
    );

    Task<IEnumerable<DbItem>> GetNearbyItemsAsync(
        Point origin,
        double radiusMeters,
        string? category,
        int page,
        int pageSize
    );

    Task<DbItem?> GetItemAsync(int id);

    Task<DbItem> CreateItemAsync(
        string title,
        string? description,
        double dailyRate,
        int categoryId,
        int ownerId,
        Point location
    );

    Task<DbItem> UpdateItemAsync(
        int id,
        string? title,
        string? description,
        double? dailyRate,
        bool? isAvailable
    );
}
