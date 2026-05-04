using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;
using RentalApp.Database.Repositories;
using RentalApp.Database.States;
using RentalApp.Services.Auth;
using DbReview = RentalApp.Database.Models.Review;

namespace RentalApp.Services.Reviews;

/// <summary>
/// Repository-backed implementation of <see cref="IReviewService"/> for local/offline development.
/// </summary>
internal class LocalReviewService : IReviewService
{
    private readonly IReviewRepository _reviewRepository;
    private readonly IRentalRepository _rentalRepository;
    private readonly AuthTokenState _tokenState;

    public LocalReviewService(
        IReviewRepository reviewRepository,
        IRentalRepository rentalRepository,
        AuthTokenState tokenState
    )
    {
        _reviewRepository = reviewRepository;
        _rentalRepository = rentalRepository;
        _tokenState = tokenState;
    }

    /// <inheritdoc/>
    public async Task<ReviewsResponse> GetItemReviewsAsync(int itemId, GetReviewsRequest request)
    {
        var (reviews, total) = await _reviewRepository.GetItemReviewsPagedAsync(
            itemId,
            request.Page,
            request.PageSize
        );
        return ToReviewsResponse(reviews.ToList(), total, request);
    }

    /// <inheritdoc/>
    public async Task<ReviewsResponse> GetUserReviewsAsync(int userId, GetReviewsRequest request)
    {
        var (reviews, total) = await _reviewRepository.GetUserReviewsPagedAsync(
            userId,
            request.Page,
            request.PageSize
        );
        return ToReviewsResponse(reviews.ToList(), total, request);
    }

    /// <inheritdoc/>
    public async Task<CreateReviewResponse> CreateReviewAsync(CreateReviewRequest request)
    {
        if (request.Rating < 1 || request.Rating > 5)
            throw new InvalidOperationException("Rating must be between 1 and 5.");

        if (await _reviewRepository.HasReviewForRentalAsync(request.RentalId))
            throw new InvalidOperationException(
                "A review has already been submitted for this rental."
            );

        var rental =
            await _rentalRepository.GetRentalAsync(request.RentalId)
            ?? throw new InvalidOperationException($"Rental {request.RentalId} not found.");

        if (rental.Status != RentalStatus.Completed)
            throw new InvalidOperationException(
                "Reviews can only be submitted for completed rentals."
            );

        var reviewerId = GetCurrentUserId();

        if (reviewerId != rental.BorrowerId)
            throw new InvalidOperationException("Only the borrower can leave a review.");

        var review = await _reviewRepository.CreateReviewAsync(
            request.RentalId,
            rental.ItemId,
            reviewerId,
            request.Rating,
            request.Comment
        );

        return new CreateReviewResponse(
            review.Id,
            review.RentalId,
            review.ReviewerId,
            $"{review.Reviewer.FirstName} {review.Reviewer.LastName}",
            review.Rating,
            review.Comment,
            review.CreatedAt ?? DateTime.UtcNow
        );
    }

    private int GetCurrentUserId() =>
        int.Parse(
            _tokenState.CurrentToken
                ?? throw new InvalidOperationException("No user is authenticated.")
        );

    private static ReviewsResponse ToReviewsResponse(
        List<DbReview> reviews,
        int total,
        GetReviewsRequest request
    )
    {
        var mapped = reviews
            .Select(r => new ReviewResponse(
                r.Id,
                r.Rating,
                r.Comment,
                $"{r.Reviewer.FirstName} {r.Reviewer.LastName}",
                r.CreatedAt ?? DateTime.UtcNow
            ))
            .ToList();
        var totalPages = (int)Math.Ceiling(total / (double)request.PageSize);
        var avg = reviews.Count > 0 ? reviews.Average(r => r.Rating) : (double?)null;
        return new ReviewsResponse(mapped, avg, total, request.Page, request.PageSize, totalPages);
    }
}
