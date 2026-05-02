namespace RentalApp.Contracts.Requests;

/// <summary>Pagination parameters for retrieving a list of reviews.</summary>
public record GetReviewsRequest(int Page = 1, int PageSize = 10);

/// <summary>Request payload for submitting a review for a completed rental.</summary>
/// <param name="RentalId">The ID of the completed rental being reviewed.</param>
/// <param name="Rating">Star rating from 1 (lowest) to 5 (highest).</param>
/// <param name="Comment">Optional free-text comment accompanying the rating.</param>
public record CreateReviewRequest(int RentalId, int Rating, string? Comment);
