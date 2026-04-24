namespace RentalApp.Models;

/// <summary>
/// Represents a rental item listing returned by the API.
/// </summary>
/// <remarks>
/// A single type covers list, nearby-search, and detail responses. Fields not
/// returned by a particular endpoint are <see langword="null"/> — for example,
/// <see cref="Distance"/> is only populated by <c>GET /items/nearby</c>, and
/// <see cref="Reviews"/> only by <c>GET /items/{id}</c>.
/// </remarks>
/// <param name="Id">Unique item identifier.</param>
/// <param name="Title">Listing title.</param>
/// <param name="Description">Optional description of the item.</param>
/// <param name="DailyRate">Daily rental rate.</param>
/// <param name="CategoryId">Identifier of the item's category.</param>
/// <param name="Category">Display name of the item's category.</param>
/// <param name="OwnerId">User ID of the item owner.</param>
/// <param name="OwnerName">Display name of the item owner.</param>
/// <param name="OwnerRating">Average rating of the owner; <see langword="null"/> if the owner has no reviews.</param>
/// <param name="Latitude">Geographic latitude; present in nearby and detail responses, <see langword="null"/> in list responses.</param>
/// <param name="Longitude">Geographic longitude; present in nearby and detail responses, <see langword="null"/> in list responses.</param>
/// <param name="Distance">Distance in kilometres from the search origin; only present in nearby-search responses.</param>
/// <param name="IsAvailable">Whether the item is currently available for rental.</param>
/// <param name="AverageRating">Average rating across all reviews; <see langword="null"/> if the item has no reviews.</param>
/// <param name="TotalReviews">Total number of reviews; only present in detail responses.</param>
/// <param name="CreatedAt">Timestamp when the listing was created; present in list and create responses, <see langword="null"/> in nearby responses.</param>
/// <param name="Reviews">Reviews for this item; only present in detail responses.</param>
public sealed record Item(
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
    double? Distance,
    bool IsAvailable,
    double? AverageRating,
    int? TotalReviews,
    DateTime? CreatedAt,
    List<Review>? Reviews
);
