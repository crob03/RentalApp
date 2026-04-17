namespace RentalApp.Models;

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
