using RentalApp.Database.Models;

namespace RentalApp.Database.Repositories;

public interface IReviewRepository
{
    /// <summary>Returns a paged result of reviews for a specific item, ordered newest first.</summary>
    Task<(IEnumerable<Review> Reviews, int Total)> GetItemReviewsPagedAsync(
        int itemId,
        int page,
        int pageSize
    );

    /// <summary>Returns a paged result of reviews written by a specific user, ordered newest first.</summary>
    Task<(IEnumerable<Review> Reviews, int Total)> GetUserReviewsPagedAsync(
        int userId,
        int page,
        int pageSize
    );

    /// <summary>Persists a new review and returns it with navigation properties loaded.</summary>
    Task<Review> CreateReviewAsync(
        int rentalId,
        int itemId,
        int reviewerId,
        int rating,
        string? comment
    );

    /// <summary>Returns true if a review already exists for the given rental.</summary>
    Task<bool> HasReviewForRentalAsync(int rentalId);
}
