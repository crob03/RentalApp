using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;

namespace RentalApp.Services;

/// <summary>
/// Stub implementation of <see cref="IReviewService"/> for local/offline mode.
/// All methods throw <see cref="NotImplementedException"/> — Review DB entities are not yet implemented.
/// </summary>
internal class LocalReviewService : IReviewService
{
    /// <inheritdoc/>
    public Task<ReviewsResponse> GetItemReviewsAsync(int itemId, GetReviewsRequest request) =>
        throw new NotImplementedException("Review support requires local DB entities");

    /// <inheritdoc/>
    public Task<ReviewsResponse> GetUserReviewsAsync(int userId, GetReviewsRequest request) =>
        throw new NotImplementedException("Review support requires local DB entities");

    /// <inheritdoc/>
    public Task<CreateReviewResponse> CreateReviewAsync(CreateReviewRequest request) =>
        throw new NotImplementedException("Review support requires local DB entities");
}
