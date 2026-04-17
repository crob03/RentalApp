namespace RentalApp.Models;

public sealed record User(
    int Id,
    string FirstName,
    string LastName,
    double? AverageRating,
    int ItemsListed,
    int RentalsCompleted,
    string? Email,
    DateTime? CreatedAt,
    List<Review>? Reviews
);
