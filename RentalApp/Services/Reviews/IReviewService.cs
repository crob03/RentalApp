using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;

namespace RentalApp.Services.Reviews;

/// <summary>
/// Defines the contract for submitting and retrieving rental reviews.
/// </summary>
public interface IReviewService
{
    /// <summary>
    /// Returns a paginated list of reviews written about a specific item.
    /// </summary>
    /// <param name="itemId">The unique identifier of the item whose reviews are requested.</param>
    /// <param name="request">Pagination parameters (page, page size).</param>
    /// <returns>A page of reviews for the item together with pagination metadata.</returns>
    Task<ReviewsResponse> GetItemReviewsAsync(int itemId, GetReviewsRequest request);

    /// <summary>
    /// Returns a paginated list of reviews received by a specific user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose reviews are requested.</param>
    /// <param name="request">Pagination parameters (page, page size).</param>
    /// <returns>A page of reviews for the user together with pagination metadata.</returns>
    Task<ReviewsResponse> GetUserReviewsAsync(int userId, GetReviewsRequest request);

    /// <summary>
    /// Submits a new review for a completed rental.
    /// </summary>
    /// <param name="request">The rental identifier, numeric rating, and optional comment text.</param>
    /// <returns>The persisted review including its assigned identifier and timestamp.</returns>
    Task<CreateReviewResponse> CreateReviewAsync(CreateReviewRequest request);
}
