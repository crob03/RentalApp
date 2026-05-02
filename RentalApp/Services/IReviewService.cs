using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;

namespace RentalApp.Services;

public interface IReviewService
{
    Task<ReviewsResponse> GetItemReviewsAsync(int itemId, GetReviewsRequest request);
    Task<ReviewsResponse> GetUserReviewsAsync(int userId, GetReviewsRequest request);
    Task<CreateReviewResponse> CreateReviewAsync(CreateReviewRequest request);
}
