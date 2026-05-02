// RentalApp/Services/IApiService.cs
using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;

namespace RentalApp.Services;

/// <summary>
/// Low-level API abstraction covering auth, item, category, rental, and review operations.
/// Implemented by <see cref="RemoteApiService"/> (HTTP) and <see cref="LocalApiService"/> (local DB).
/// </summary>
public interface IApiService
{
    // Auth

    /// <summary>
    /// Authenticates a user and returns a bearer token.
    /// </summary>
    /// <param name="request">The login credentials.</param>
    Task<LoginResponse> LoginAsync(LoginRequest request);

    /// <summary>
    /// Registers a new user account.
    /// </summary>
    /// <param name="request">The registration details.</param>
    Task<RegisterResponse> RegisterAsync(RegisterRequest request);

    /// <summary>
    /// Returns profile information for the currently authenticated user.
    /// </summary>
    Task<CurrentUserResponse> GetCurrentUserAsync();

    /// <summary>
    /// Returns the public profile for the specified user.
    /// </summary>
    /// <param name="userId">The ID of the user to retrieve.</param>
    Task<UserProfileResponse> GetUserProfileAsync(int userId);

    // Items

    /// <summary>
    /// Returns a paginated list of items, optionally filtered by category and search text.
    /// </summary>
    /// <param name="request">Pagination, category, and search parameters.</param>
    Task<ItemsResponse> GetItemsAsync(GetItemsRequest request);

    /// <summary>
    /// Returns items within the specified radius of a geographic coordinate.
    /// </summary>
    /// <param name="request">Location, radius, and optional category filter.</param>
    Task<NearbyItemsResponse> GetNearbyItemsAsync(GetNearbyItemsRequest request);

    /// <summary>
    /// Returns detailed information for a single item.
    /// </summary>
    /// <param name="id">The item ID.</param>
    Task<ItemDetailResponse> GetItemAsync(int id);

    /// <summary>
    /// Creates a new rental item owned by the currently authenticated user.
    /// </summary>
    /// <param name="request">The item details.</param>
    Task<CreateItemResponse> CreateItemAsync(CreateItemRequest request);

    /// <summary>
    /// Updates the mutable fields of an existing item.
    /// </summary>
    /// <param name="id">The item ID.</param>
    /// <param name="request">The updated field values.</param>
    Task<UpdateItemResponse> UpdateItemAsync(int id, UpdateItemRequest request);

    // Categories

    /// <summary>
    /// Returns all available item categories with their item counts.
    /// </summary>
    Task<CategoriesResponse> GetCategoriesAsync();

    // Rentals — LocalApiService throws NotImplementedException until DB entities land

    /// <summary>
    /// Returns a paginated list of incoming rentals (where the current user is the owner).
    /// </summary>
    /// <param name="request">Pagination and optional status filter.</param>
    Task<RentalsListResponse> GetIncomingRentalsAsync(GetRentalsRequest request);

    /// <summary>
    /// Returns a paginated list of outgoing rentals (where the current user is the renter).
    /// </summary>
    /// <param name="request">Pagination and optional status filter.</param>
    Task<RentalsListResponse> GetOutgoingRentalsAsync(GetRentalsRequest request);

    /// <summary>
    /// Returns detailed information for a single rental.
    /// </summary>
    /// <param name="id">The rental ID.</param>
    Task<RentalDetailResponse> GetRentalAsync(int id);

    /// <summary>
    /// Creates a new rental request for an item.
    /// </summary>
    /// <param name="request">The rental details including item, start date, and end date.</param>
    Task<RentalSummaryResponse> CreateRentalAsync(CreateRentalRequest request);

    /// <summary>
    /// Updates the status of an existing rental (e.g., approve, reject, complete).
    /// </summary>
    /// <param name="id">The rental ID.</param>
    /// <param name="request">The new status value.</param>
    Task<UpdateRentalStatusResponse> UpdateRentalStatusAsync(
        int id,
        UpdateRentalStatusRequest request
    );

    // Reviews — LocalApiService throws NotImplementedException until DB entities land

    /// <summary>
    /// Returns paginated reviews for the specified item.
    /// </summary>
    /// <param name="itemId">The item ID.</param>
    /// <param name="request">Pagination parameters.</param>
    Task<ReviewsResponse> GetItemReviewsAsync(int itemId, GetReviewsRequest request);

    /// <summary>
    /// Returns paginated reviews written about the specified user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="request">Pagination parameters.</param>
    Task<ReviewsResponse> GetUserReviewsAsync(int userId, GetReviewsRequest request);

    /// <summary>
    /// Submits a review for a completed rental.
    /// </summary>
    /// <param name="request">The review details including rental ID, rating, and comment.</param>
    Task<CreateReviewResponse> CreateReviewAsync(CreateReviewRequest request);
}
