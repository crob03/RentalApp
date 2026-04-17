namespace RentalApp.Models;

public sealed record UserProfile(
    int Id,
    string FirstName,
    string LastName,
    double? AverageRating,
    int ItemsListed,
    int RentalsCompleted,
    string? Email,
    DateTime? CreatedAt,
    List<UserReview>? Reviews
);
