namespace RentalApp.Contracts.Requests;

/// <summary>
/// Pagination and filter parameters for retrieving a list of items.
/// </summary>
/// <param name="Page">One-based page number; defaults to 1.</param>
/// <param name="PageSize">Number of items per page; defaults to 20.</param>
/// <param name="Category">When provided, only items in this category slug are returned.</param>
/// <param name="Search">When provided, only items whose title or description match are returned.</param>
public record GetItemsRequest(
    int Page = 1,
    int PageSize = 20,
    string? Category = null,
    string? Search = null
);

/// <summary>
/// Location and filter parameters for retrieving items near a geographic coordinate.
/// </summary>
/// <param name="Lat">Latitude of the search origin in decimal degrees (WGS-84).</param>
/// <param name="Lon">Longitude of the search origin in decimal degrees (WGS-84).</param>
/// <param name="Radius">Search radius in kilometres; defaults to 5 km.</param>
/// <param name="Category">When provided, only items in this category slug are returned.</param>
public record GetNearbyItemsRequest(
    double Lat,
    double Lon,
    double Radius = 5.0,
    string? Category = null
);

/// <summary>Request payload for creating a new item listing.</summary>
/// <param name="DailyRate">Rental price per day in GBP.</param>
/// <param name="Latitude">Item location latitude in decimal degrees (WGS-84).</param>
/// <param name="Longitude">Item location longitude in decimal degrees (WGS-84).</param>
public record CreateItemRequest(
    string Title,
    string? Description,
    double DailyRate,
    int CategoryId,
    double Latitude,
    double Longitude
);

/// <summary>
/// Request payload for updating an existing item listing.
/// Only non-<see langword="null"/> fields are applied; omitted fields are left unchanged.
/// </summary>
public record UpdateItemRequest(
    string? Title,
    string? Description,
    double? DailyRate,
    bool? IsAvailable
);
