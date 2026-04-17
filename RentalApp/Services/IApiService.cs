using RentalApp.Models;

namespace RentalApp.Services;

/// <summary>
/// Data-transport interface for the rental API. Implementations switch between
/// the remote HTTP backend (<see cref="RemoteApiService"/>) and the local
/// database backend (<see cref="LocalApiService"/>).
/// </summary>
/// <remarks>All methods return <see cref="RentalApp.Models"/> DTOs — never EF entities.</remarks>
public interface IApiService
{
    // ── Authentication ─────────────────────────────────────────────

    /// <summary>Authenticates the user and establishes a session.</summary>
    /// <param name="email">User's email address.</param>
    /// <param name="password">User's password.</param>
    /// <exception cref="UnauthorizedAccessException">Thrown when credentials are invalid.</exception>
    Task LoginAsync(string email, string password);

    /// <summary>Registers a new user account.</summary>
    /// <param name="firstName">First name.</param>
    /// <param name="lastName">Last name.</param>
    /// <param name="email">Email address.</param>
    /// <param name="password">Password (minimum 8 characters, must include uppercase, lowercase, digit, and special character).</param>
    /// <exception cref="InvalidOperationException">Thrown when the email is already registered.</exception>
    Task RegisterAsync(string firstName, string lastName, string email, string password);

    /// <summary>Returns the full profile of the currently authenticated user.</summary>
    Task<User> GetCurrentUserAsync();

    /// <summary>Returns the public profile of the specified user.</summary>
    /// <param name="userId">Identifier of the user to retrieve.</param>
    Task<User> GetUserAsync(int userId);

    /// <summary>Ends the current session and clears any stored session state.</summary>
    Task LogoutAsync();

    // ── Items ───────────────────────────────────────────────────────

    /// <summary>Returns a paginated list of available items, optionally filtered.</summary>
    /// <param name="category">Category slug to filter by.</param>
    /// <param name="search">Free-text search term.</param>
    /// <param name="page">Page number (1-based).</param>
    Task<List<Item>> GetItemsAsync(string? category = null, string? search = null, int page = 1);

    /// <summary>Returns items within a given radius of a geographic location.</summary>
    /// <param name="lat">Latitude of the search origin.</param>
    /// <param name="lon">Longitude of the search origin.</param>
    /// <param name="radius">Search radius in kilometres (default 5, max 50).</param>
    /// <param name="category">Category slug to filter by.</param>
    Task<List<Item>> GetNearbyItemsAsync(
        double lat,
        double lon,
        double radius = 5.0,
        string? category = null
    );

    /// <summary>Returns full details for the specified item, including reviews.</summary>
    /// <param name="id">Item identifier.</param>
    Task<Item> GetItemAsync(int id);

    /// <summary>Creates a new item listing owned by the authenticated user.</summary>
    /// <param name="title">Listing title (5–100 characters).</param>
    /// <param name="description">Optional description (max 1000 characters).</param>
    /// <param name="dailyRate">Daily rental rate (must be positive, max 1000).</param>
    /// <param name="categoryId">Category identifier.</param>
    /// <param name="latitude">Latitude of the item's location.</param>
    /// <param name="longitude">Longitude of the item's location.</param>
    Task<Item> CreateItemAsync(
        string title,
        string? description,
        double dailyRate,
        int categoryId,
        double latitude,
        double longitude
    );

    /// <summary>Updates mutable fields on an item owned by the authenticated user.</summary>
    /// <param name="id">Item identifier.</param>
    /// <param name="title">New title; <see langword="null"/> leaves the field unchanged.</param>
    /// <param name="description">New description; <see langword="null"/> leaves the field unchanged.</param>
    /// <param name="dailyRate">New daily rate; <see langword="null"/> leaves the field unchanged.</param>
    /// <param name="isAvailable">New availability flag; <see langword="null"/> leaves the field unchanged.</param>
    Task<Item> UpdateItemAsync(
        int id,
        string? title,
        string? description,
        double? dailyRate,
        bool? isAvailable
    );

    // ── Categories ──────────────────────────────────────────────────

    /// <summary>Returns all available item categories.</summary>
    Task<List<Category>> GetCategoriesAsync();

    // ── Rentals ─────────────────────────────────────────────────────

    /// <summary>Creates a rental request for the specified item.</summary>
    /// <param name="itemId">Item to rent.</param>
    /// <param name="startDate">First day of the rental period.</param>
    /// <param name="endDate">Last day of the rental period.</param>
    Task<Rental> RequestRentalAsync(int itemId, DateOnly startDate, DateOnly endDate);

    /// <summary>Returns incoming rental requests for items owned by the authenticated user.</summary>
    /// <param name="status">Optional status filter (e.g. <c>pending</c>, <c>approved</c>).</param>
    Task<List<Rental>> GetIncomingRentalsAsync(string? status = null);

    /// <summary>Returns outgoing rental requests made by the authenticated user.</summary>
    /// <param name="status">Optional status filter.</param>
    Task<List<Rental>> GetOutgoingRentalsAsync(string? status = null);

    /// <summary>Returns full details for the specified rental.</summary>
    /// <param name="id">Rental identifier.</param>
    Task<Rental> GetRentalAsync(int id);

    /// <summary>Transitions the specified rental to a new status.</summary>
    /// <param name="rentalId">Rental identifier.</param>
    /// <param name="status">Target status string.</param>
    Task UpdateRentalStatusAsync(int rentalId, string status);

    // ── Reviews ─────────────────────────────────────────────────────

    /// <summary>Submits a review for a completed rental.</summary>
    /// <param name="rentalId">Identifier of the completed rental.</param>
    /// <param name="rating">Rating from 1 to 5.</param>
    /// <param name="comment">Optional written comment (max 500 characters).</param>
    Task<Review> CreateReviewAsync(int rentalId, int rating, string comment);

    /// <summary>Returns paginated reviews for the specified item.</summary>
    /// <param name="itemId">Item identifier.</param>
    /// <param name="page">Page number (1-based).</param>
    Task<List<Review>> GetItemReviewsAsync(int itemId, int page = 1);

    /// <summary>Returns paginated reviews received by the specified user as an owner.</summary>
    /// <param name="userId">User identifier.</param>
    /// <param name="page">Page number (1-based).</param>
    Task<List<Review>> GetUserReviewsAsync(int userId, int page = 1);
}
