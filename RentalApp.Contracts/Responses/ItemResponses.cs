namespace RentalApp.Contracts.Responses;

public record ItemsResponse(
    List<ItemSummaryResponse> Items,
    int TotalItems,
    int Page,
    int PageSize,
    int TotalPages
);

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

public record NearbyItemsResponse(
    List<NearbyItemResponse> Items,
    SearchLocationResponse SearchLocation,
    double Radius,
    int TotalResults
);

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

public record UpdateItemResponse(
    int Id,
    string Title,
    string? Description,
    double DailyRate,
    bool IsAvailable
);

public record SearchLocationResponse(double Latitude, double Longitude);
