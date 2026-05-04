using Microsoft.EntityFrameworkCore;
using RentalApp.Contracts.Requests;
using RentalApp.Database.Data;
using RentalApp.Database.Models;
using RentalApp.Database.Repositories;
using RentalApp.Database.States;
using RentalApp.Services.Auth;
using RentalApp.Services.Reviews;
using RentalApp.Test.Fixtures;

namespace RentalApp.Test.Services;

public class LocalReviewServiceTests
    : IClassFixture<DatabaseFixture<LocalReviewServiceTests>>,
        IAsyncLifetime
{
    private readonly DatabaseFixture<LocalReviewServiceTests> _fixture;
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly AuthTokenState _tokenState = new();

    private const int OwnerId = 1;
    private const int BorrowerId = 2;
    private const int ItemId = 1;

    public LocalReviewServiceTests(DatabaseFixture<LocalReviewServiceTests> fixture)
    {
        _fixture = fixture;
        _contextFactory = fixture.ContextFactory;
    }

    public async Task InitializeAsync()
    {
        _tokenState.CurrentToken = null;
        await _fixture.ResetRentalsAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private LocalReviewService CreateSut() =>
        new(
            new ReviewRepository(_contextFactory),
            new RentalRepository(_contextFactory),
            _tokenState
        );

    private async Task<int> CreateCompletedRentalAsync()
    {
        await using var context = _contextFactory.CreateDbContext();
        var rental = new Rental
        {
            ItemId = ItemId,
            OwnerId = OwnerId,
            BorrowerId = BorrowerId,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-10)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-3)),
            Status = RentalStatus.Completed,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        context.Rentals.Add(rental);
        await context.SaveChangesAsync();
        return rental.Id;
    }

    [Fact]
    public async Task GetItemReviewsAsync_ReturnsReviewsWithCorrectAverageRating()
    {
        var rentalId = await CreateCompletedRentalAsync();
        _tokenState.CurrentToken = BorrowerId.ToString();
        await CreateSut().CreateReviewAsync(new CreateReviewRequest(rentalId, 4, "Good"));

        var result = await CreateSut().GetItemReviewsAsync(ItemId, new GetReviewsRequest(1, 10));

        Assert.Equal(1, result.TotalReviews);
        Assert.Equal(4.0, result.AverageRating);
        Assert.Single(result.Reviews);
    }

    [Fact]
    public async Task GetItemReviewsAsync_NoReviews_ReturnsNullAverageRating()
    {
        var result = await CreateSut().GetItemReviewsAsync(ItemId, new GetReviewsRequest(1, 10));

        Assert.Equal(0, result.TotalReviews);
        Assert.Null(result.AverageRating);
        Assert.Empty(result.Reviews);
    }

    [Fact]
    public async Task GetUserReviewsAsync_ReturnsOwnerReviews()
    {
        var rentalId = await CreateCompletedRentalAsync();
        _tokenState.CurrentToken = BorrowerId.ToString();
        await CreateSut().CreateReviewAsync(new CreateReviewRequest(rentalId, 5, null));

        var result = await CreateSut()
            .GetUserReviewsAsync(OwnerId, new GetReviewsRequest(1, 10));

        Assert.Equal(1, result.TotalReviews);
        Assert.Single(result.Reviews);
    }

    [Fact]
    public async Task CreateReviewAsync_ValidRequest_ReturnsCreatedReview()
    {
        var rentalId = await CreateCompletedRentalAsync();
        _tokenState.CurrentToken = BorrowerId.ToString();

        var result = await CreateSut()
            .CreateReviewAsync(new CreateReviewRequest(rentalId, 5, "Excellent!"));

        Assert.Equal(rentalId, result.RentalId);
        Assert.Equal(BorrowerId, result.ReviewerId);
        Assert.Equal(5, result.Rating);
        Assert.Equal("Excellent!", result.Comment);
    }

    [Fact]
    public async Task CreateReviewAsync_DuplicateReview_ThrowsInvalidOperation()
    {
        var rentalId = await CreateCompletedRentalAsync();
        _tokenState.CurrentToken = BorrowerId.ToString();
        await CreateSut().CreateReviewAsync(new CreateReviewRequest(rentalId, 5, null));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateSut().CreateReviewAsync(new CreateReviewRequest(rentalId, 3, "Changed mind"))
        );
    }

    [Fact]
    public async Task CreateReviewAsync_RatingTooLow_ThrowsInvalidOperation()
    {
        var rentalId = await CreateCompletedRentalAsync();
        _tokenState.CurrentToken = BorrowerId.ToString();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateSut().CreateReviewAsync(new CreateReviewRequest(rentalId, 0, null))
        );
    }

    [Fact]
    public async Task CreateReviewAsync_RatingTooHigh_ThrowsInvalidOperation()
    {
        var rentalId = await CreateCompletedRentalAsync();
        _tokenState.CurrentToken = BorrowerId.ToString();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateSut().CreateReviewAsync(new CreateReviewRequest(rentalId, 6, null))
        );
    }
}
