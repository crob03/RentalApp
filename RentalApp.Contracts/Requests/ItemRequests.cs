namespace RentalApp.Contracts.Requests;

public record GetItemsRequest(
    string? Category = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 20
);

public record GetNearbyItemsRequest(
    double Lat,
    double Lon,
    double Radius = 5.0,
    string? Category = null
);

public record CreateItemRequest(
    string Title,
    string? Description,
    double DailyRate,
    int CategoryId,
    double Latitude,
    double Longitude
);

public record UpdateItemRequest(
    string? Title,
    string? Description,
    double? DailyRate,
    bool? IsAvailable
);
