namespace RentalApp.Models;

/// <summary>
/// Represents an authenticated or public user profile returned by the API.
/// </summary>
/// <param name="Id">Unique user identifier.</param>
/// <param name="FirstName">User's first name.</param>
/// <param name="LastName">User's last name.</param>
/// <param name="AverageRating">Average owner rating across completed rentals; <see langword="null"/> if the user has no reviews.</param>
/// <param name="ItemsListed">Total number of items the user has listed.</param>
/// <param name="RentalsCompleted">Total number of rentals the user has completed as owner.</param>
/// <param name="Email">Email address; present on the authenticated user's own profile (<c>/users/me</c>), <see langword="null"/> on public profiles.</param>
/// <param name="CreatedAt">Account creation timestamp; present on the authenticated user's own profile, <see langword="null"/> on public profiles.</param>
/// <param name="Reviews">Reviews received as an owner; present on public profiles (<c>/users/{id}/profile</c>), <see langword="null"/> on the current user's own profile.</param>
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
