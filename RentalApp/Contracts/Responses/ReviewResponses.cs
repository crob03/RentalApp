namespace RentalApp.Contracts.Responses;

public record ReviewResponse(
    int Id,
    int Rating,
    string? Comment,
    string ReviewerName,
    DateTime CreatedAt
);

public record ItemReviewResponse(
    int Id,
    int ReviewerId,
    string ReviewerName,
    int Rating,
    string? Comment,
    DateTime CreatedAt
);

public record ReviewsResponse(
    List<ReviewResponse> Reviews,
    double? AverageRating,
    int TotalReviews,
    int Page,
    int PageSize,
    int TotalPages
);

public record CreateReviewResponse(
    int Id,
    int RentalId,
    int ReviewerId,
    string ReviewerName,
    int Rating,
    string? Comment,
    DateTime CreatedAt
);
