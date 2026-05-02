namespace RentalApp.Contracts.Responses;

/// <summary>Profile of the currently authenticated user.</summary>
/// <param name="AverageRating">Average rating across all reviews received, or <see langword="null"/> if the user has no reviews.</param>
public record CurrentUserResponse(
    int Id,
    string Email,
    string FirstName,
    string LastName,
    double? AverageRating,
    int ItemsListed,
    int RentalsCompleted,
    DateTime CreatedAt
);

/// <summary>Public profile of a user as seen by other users.</summary>
/// <param name="AverageRating">Average rating across all reviews received, or <see langword="null"/> if the user has no reviews.</param>
public record UserProfileResponse(
    int Id,
    string FirstName,
    string LastName,
    double? AverageRating,
    int ItemsListed,
    int RentalsCompleted,
    List<ReviewResponse> Reviews
);
