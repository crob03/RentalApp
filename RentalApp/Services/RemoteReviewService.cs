using System.Net.Http.Json;
using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;
using RentalApp.Http;
using static System.FormattableString;

namespace RentalApp.Services;

internal class RemoteReviewService : RemoteServiceBase, IReviewService
{
    private readonly IApiClient _apiClient;

    public RemoteReviewService(IApiClient apiClient) => _apiClient = apiClient;

    public async Task<ReviewsResponse> GetItemReviewsAsync(int itemId, GetReviewsRequest request)
    {
        var response = await _apiClient.GetAsync(
            Invariant($"items/{itemId}/reviews?page={request.Page}&pageSize={request.PageSize}")
        );
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<ReviewsResponse>()
            ?? throw new InvalidOperationException("Empty reviews response from API");
    }

    public async Task<ReviewsResponse> GetUserReviewsAsync(int userId, GetReviewsRequest request)
    {
        var response = await _apiClient.GetAsync(
            Invariant($"users/{userId}/reviews?page={request.Page}&pageSize={request.PageSize}")
        );
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<ReviewsResponse>()
            ?? throw new InvalidOperationException("Empty reviews response from API");
    }

    public async Task<CreateReviewResponse> CreateReviewAsync(CreateReviewRequest request)
    {
        var response = await _apiClient.PostAsJsonAsync(
            "reviews",
            new
            {
                rentalId = request.RentalId,
                rating = request.Rating,
                comment = request.Comment,
            }
        );
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<CreateReviewResponse>()
            ?? throw new InvalidOperationException("Empty create review response from API");
    }
}
