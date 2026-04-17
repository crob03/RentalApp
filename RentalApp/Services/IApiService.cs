using RentalApp.Models;

namespace RentalApp.Services;

public interface IApiService
{
    // Authentication
    Task LoginAsync(string email, string password);
    Task RegisterAsync(string firstName, string lastName, string email, string password);
    Task<UserProfile> GetCurrentUserAsync();
    Task<UserProfile> GetUserProfileAsync(int userId);
    Task LogoutAsync();

    // Items
    Task<List<Item>> GetItemsAsync(string? category = null, string? search = null, int page = 1);
    Task<List<Item>> GetNearbyItemsAsync(
        double lat,
        double lon,
        double radius = 5.0,
        string? category = null
    );
    Task<Item> GetItemAsync(int id);
    Task<Item> CreateItemAsync(CreateItemRequest request);
    Task<Item> UpdateItemAsync(int id, UpdateItemRequest request);

    // Categories
    Task<List<Category>> GetCategoriesAsync();

    // Rentals
    Task<Rental> RequestRentalAsync(int itemId, DateOnly startDate, DateOnly endDate);
    Task<List<Rental>> GetIncomingRentalsAsync(string? status = null);
    Task<List<Rental>> GetOutgoingRentalsAsync(string? status = null);
    Task<Rental> GetRentalAsync(int id);
    Task UpdateRentalStatusAsync(int rentalId, string status);

    // Reviews
    Task<Review> CreateReviewAsync(int rentalId, int rating, string comment);
    Task<List<Review>> GetItemReviewsAsync(int itemId, int page = 1);
    Task<List<Review>> GetUserReviewsAsync(int userId, int page = 1);
}
