using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;

namespace RentalApp.Services;

internal class LocalReviewService : IReviewService
{
    public Task<ReviewsResponse> GetItemReviewsAsync(int itemId, GetReviewsRequest request) =>
        throw new NotImplementedException("Review support requires local DB entities");

    public Task<ReviewsResponse> GetUserReviewsAsync(int userId, GetReviewsRequest request) =>
        throw new NotImplementedException("Review support requires local DB entities");

    public Task<CreateReviewResponse> CreateReviewAsync(CreateReviewRequest request) =>
        throw new NotImplementedException("Review support requires local DB entities");
}
