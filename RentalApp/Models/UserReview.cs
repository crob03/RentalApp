namespace RentalApp.Models;

public sealed record UserReview(
    int Id,
    int? RentalId,
    int? ItemId,
    int ReviewerId,
    int Rating,
    string? ItemTitle,
    string Comment,
    string ReviewerName,
    DateTime CreatedAt
);
