namespace RentalApp.Contracts.Responses;

/// <summary>A single review as it appears in user-facing review lists.</summary>
/// <param name="Rating">Star rating from 1 (lowest) to 5 (highest).</param>
public record ReviewResponse(
    int Id,
    int Rating,
    string? Comment,
    string ReviewerName,
    DateTime CreatedAt
);

/// <summary>A single review as it appears embedded within an item detail response.</summary>
/// <param name="Rating">Star rating from 1 (lowest) to 5 (highest).</param>
public record ItemReviewResponse(
    int Id,
    int ReviewerId,
    string ReviewerName,
    int Rating,
    string? Comment,
    DateTime CreatedAt
);

/// <summary>Paginated list of reviews with aggregate rating information.</summary>
/// <param name="AverageRating">Mean rating across all reviews for this subject, or <see langword="null"/> if there are no reviews.</param>
public record ReviewsResponse(
    List<ReviewResponse> Reviews,
    double? AverageRating,
    int TotalReviews,
    int Page,
    int PageSize,
    int TotalPages
);

/// <summary>Response returned after a review is successfully submitted.</summary>
/// <param name="Rating">Star rating from 1 (lowest) to 5 (highest).</param>
public record CreateReviewResponse(
    int Id,
    int RentalId,
    int ReviewerId,
    string ReviewerName,
    int Rating,
    string? Comment,
    DateTime CreatedAt
);
