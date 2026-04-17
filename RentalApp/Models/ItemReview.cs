namespace RentalApp.Models;

public sealed record ItemReview(
    int Id,
    int ReviewerId,
    string ReviewerName,
    int Rating,
    string? Comment,
    DateTime CreatedAt
);
