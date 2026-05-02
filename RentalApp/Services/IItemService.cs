using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;

namespace RentalApp.Services;

/// <summary>
/// Defines the contract for item listing, discovery, and management operations.
/// </summary>
public interface IItemService
{
    /// <summary>
    /// Returns a paginated, optionally filtered list of items ordered by creation date.
    /// </summary>
    /// <param name="request">Pagination and filter parameters (page, page size, category slug, search text).</param>
    /// <returns>A page of item summaries together with total count and pagination metadata.</returns>
    Task<ItemsResponse> GetItemsAsync(GetItemsRequest request);

    /// <summary>
    /// Returns all items within a given radius of a geographic coordinate.
    /// </summary>
    /// <param name="request">Search origin (latitude/longitude), radius in kilometres, and optional category filter.</param>
    /// <returns>Items near the origin, each decorated with their distance from the search point.</returns>
    Task<NearbyItemsResponse> GetNearbyItemsAsync(GetNearbyItemsRequest request);

    /// <summary>
    /// Returns the full detail of a single item by its identifier.
    /// </summary>
    /// <param name="id">The item's unique identifier.</param>
    /// <returns>Full item detail including reviews and owner information.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no item with the given <paramref name="id"/> exists.</exception>
    Task<ItemDetailResponse> GetItemAsync(int id);

    /// <summary>
    /// Creates a new item listing owned by the currently authenticated user.
    /// </summary>
    /// <param name="request">Title, description, daily rate, category, and geographic location of the new item.</param>
    /// <returns>The created item, including its assigned identifier and owner details.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no user is currently authenticated.</exception>
    Task<CreateItemResponse> CreateItemAsync(CreateItemRequest request);

    /// <summary>
    /// Updates the editable fields of an existing item.
    /// </summary>
    /// <param name="id">The identifier of the item to update.</param>
    /// <param name="request">Updated title, description, daily rate, and availability flag.</param>
    /// <returns>The item reflecting the applied changes.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no item with the given <paramref name="id"/> exists.</exception>
    Task<UpdateItemResponse> UpdateItemAsync(int id, UpdateItemRequest request);

    /// <summary>
    /// Returns all categories together with the count of available items in each.
    /// </summary>
    /// <returns>The full category list ordered by name.</returns>
    Task<CategoriesResponse> GetCategoriesAsync();
}
