using Microsoft.EntityFrameworkCore;
using RentalApp.Database.Data;
using RentalApp.Database.Models;

namespace RentalApp.Database.Repositories;

public class ReviewRepository : IReviewRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public ReviewRepository(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    /// <inheritdoc/>
    public async Task<(IEnumerable<Review> Reviews, int Total)> GetItemReviewsPagedAsync(
        int itemId,
        int page,
        int pageSize
    )
    {
        await using var context = _contextFactory.CreateDbContext();
        var query = context
            .Reviews.Include(r => r.Reviewer)
            .Where(r => r.ItemId == itemId)
            .OrderByDescending(r => r.CreatedAt);
        var total = await query.CountAsync();
        var reviews = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (reviews, total);
    }

    /// <inheritdoc/>
    public async Task<(IEnumerable<Review> Reviews, int Total)> GetUserReviewsPagedAsync(
        int userId,
        int page,
        int pageSize
    )
    {
        await using var context = _contextFactory.CreateDbContext();
        var query = context
            .Reviews.Include(r => r.Reviewer)
            .Where(r => r.ReviewerId == userId)
            .OrderByDescending(r => r.CreatedAt);
        var total = await query.CountAsync();
        var reviews = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (reviews, total);
    }

    /// <inheritdoc/>
    public async Task<Review> CreateReviewAsync(
        int rentalId,
        int itemId,
        int reviewerId,
        int rating,
        string? comment
    )
    {
        await using var context = _contextFactory.CreateDbContext();
        var review = new Review
        {
            RentalId = rentalId,
            ItemId = itemId,
            ReviewerId = reviewerId,
            Rating = rating,
            Comment = comment,
            CreatedAt = DateTime.UtcNow,
        };
        context.Reviews.Add(review);
        await context.SaveChangesAsync();
        return await context
            .Reviews.Include(r => r.Reviewer)
            .Include(r => r.Item)
            .Include(r => r.Rental)
            .FirstAsync(r => r.Id == review.Id);
    }

    /// <inheritdoc/>
    public async Task<bool> HasReviewForRentalAsync(int rentalId)
    {
        await using var context = _contextFactory.CreateDbContext();
        return await context.Reviews.AnyAsync(r => r.RentalId == rentalId);
    }
}
