namespace RentalApp.Models;

public sealed record UserProfile(
    int Id,
    string FirstName,
    string LastName,
    string Email,
    DateTime? CreatedAt
);
