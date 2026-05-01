namespace RentalApp.Contracts.Responses;

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

public record UserProfileResponse(
    int Id,
    string FirstName,
    string LastName,
    double? AverageRating,
    int ItemsListed,
    int RentalsCompleted,
    List<ReviewResponse> Reviews
);
