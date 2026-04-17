namespace RentalApp.Models;

public sealed record Review(
    int Id,
    int RentalId,
    int ReviewerId,
    string ReviewerName,
    int Rating,
    string? Comment,
    DateTime CreatedAt
);
