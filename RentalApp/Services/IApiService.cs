// RentalApp/Services/IApiService.cs
using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;

namespace RentalApp.Services;

public interface IApiService
{
    // Auth
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<RegisterResponse> RegisterAsync(RegisterRequest request);
    Task<CurrentUserResponse> GetCurrentUserAsync();
    Task<UserProfileResponse> GetUserProfileAsync(int userId);

    // Items
    Task<ItemsResponse> GetItemsAsync(GetItemsRequest request);
    Task<NearbyItemsResponse> GetNearbyItemsAsync(GetNearbyItemsRequest request);
    Task<ItemDetailResponse> GetItemAsync(int id);
    Task<CreateItemResponse> CreateItemAsync(CreateItemRequest request);
    Task<UpdateItemResponse> UpdateItemAsync(int id, UpdateItemRequest request);

    // Categories
    Task<CategoriesResponse> GetCategoriesAsync();

    // Rentals — LocalApiService throws NotImplementedException until DB entities land
    Task<RentalsListResponse> GetIncomingRentalsAsync(GetRentalsRequest request);
    Task<RentalsListResponse> GetOutgoingRentalsAsync(GetRentalsRequest request);
    Task<RentalDetailResponse> GetRentalAsync(int id);
    Task<RentalSummaryResponse> CreateRentalAsync(CreateRentalRequest request);
    Task<UpdateRentalStatusResponse> UpdateRentalStatusAsync(
        int id,
        UpdateRentalStatusRequest request
    );

    // Reviews — LocalApiService throws NotImplementedException until DB entities land
    Task<ReviewsResponse> GetItemReviewsAsync(int itemId, GetReviewsRequest request);
    Task<ReviewsResponse> GetUserReviewsAsync(int userId, GetReviewsRequest request);
    Task<CreateReviewResponse> CreateReviewAsync(CreateReviewRequest request);
}
