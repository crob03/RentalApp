namespace RentalApp.Contracts.Responses;

/// <summary>Paginated list of item summaries.</summary>
public record ItemsResponse(
    List<ItemSummaryResponse> Items,
    int TotalItems,
    int Page,
    int PageSize,
    int TotalPages
);

/// <summary>
/// Summary of a single item, used in list and search views.
/// Implements <see cref="IItemListable"/> for use with shared item-listing ViewModels.
/// </summary>
/// <param name="DailyRate">Rental price per day in GBP.</param>
/// <param name="OwnerRating">Average rating of the item owner, or <see langword="null"/> if the owner has no reviews.</param>
/// <param name="AverageRating">Average rating of this item, or <see langword="null"/> if the item has no reviews.</param>
public record ItemSummaryResponse(
    int Id,
    string Title,
    string? Description,
    double DailyRate,
    int CategoryId,
    string Category,
    int OwnerId,
    string OwnerName,
    double? OwnerRating,
    bool IsAvailable,
    double? AverageRating,
    DateTime CreatedAt
) : IItemListable;

/// <summary>Response containing nearby items and the search origin used to find them.</summary>
/// <param name="Radius">Search radius that was applied, in kilometres.</param>
public record NearbyItemsResponse(
    List<NearbyItemResponse> Items,
    SearchLocationResponse SearchLocation,
    double Radius,
    int TotalResults
);

/// <summary>
/// A single item returned by a proximity search.
/// Implements <see cref="IItemListable"/> for use with shared item-listing ViewModels.
/// </summary>
/// <param name="DailyRate">Rental price per day in GBP.</param>
/// <param name="Latitude">Item location latitude in decimal degrees (WGS-84).</param>
/// <param name="Longitude">Item location longitude in decimal degrees (WGS-84).</param>
/// <param name="Distance">Straight-line distance from the search origin to this item, in kilometres.</param>
/// <param name="AverageRating">Average rating of this item, or <see langword="null"/> if the item has no reviews.</param>
public record NearbyItemResponse(
    int Id,
    string Title,
    string? Description,
    double DailyRate,
    int CategoryId,
    string Category,
    int OwnerId,
    string OwnerName,
    double Latitude,
    double Longitude,
    double Distance,
    bool IsAvailable,
    double? AverageRating
) : IItemListable;

/// <summary>Full details of a single item, including its reviews.</summary>
/// <param name="DailyRate">Rental price per day in GBP.</param>
/// <param name="OwnerRating">Average rating of the item owner, or <see langword="null"/> if the owner has no reviews.</param>
/// <param name="Latitude">Item location latitude in decimal degrees (WGS-84), or <see langword="null"/> if the API did not return location data.</param>
/// <param name="Longitude">Item location longitude in decimal degrees (WGS-84), or <see langword="null"/> if the API did not return location data.</param>
/// <param name="AverageRating">Average rating of this item, or <see langword="null"/> if the item has no reviews.</param>
public record ItemDetailResponse(
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
    bool IsAvailable,
    double? AverageRating,
    int TotalReviews,
    DateTime CreatedAt,
    List<ItemReviewResponse> Reviews
);

/// <summary>Response returned after a new item listing is successfully created.</summary>
/// <param name="DailyRate">Rental price per day in GBP.</param>
/// <param name="Latitude">Item location latitude in decimal degrees (WGS-84).</param>
/// <param name="Longitude">Item location longitude in decimal degrees (WGS-84).</param>
public record CreateItemResponse(
    int Id,
    string Title,
    string? Description,
    double DailyRate,
    int CategoryId,
    string Category,
    int OwnerId,
    string OwnerName,
    double Latitude,
    double Longitude,
    bool IsAvailable,
    DateTime CreatedAt
);

/// <summary>Response returned after an item listing is successfully updated.</summary>
/// <param name="DailyRate">Rental price per day in GBP.</param>
public record UpdateItemResponse(
    int Id,
    string Title,
    string? Description,
    double DailyRate,
    bool IsAvailable
);

/// <summary>The geographic coordinate used as the origin for a proximity search.</summary>
public record SearchLocationResponse(double Latitude, double Longitude);
