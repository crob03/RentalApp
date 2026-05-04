using Microsoft.EntityFrameworkCore;
using RentalApp.Database.Data;
using RentalApp.Database.Models;
using RentalApp.Database.Repositories;
using RentalApp.Database.States;
using RentalApp.Test.Fixtures;

namespace RentalApp.Test.Repositories;

public class ReviewRepositoryTests
    : IClassFixture<DatabaseFixture<ReviewRepositoryTests>>,
        IAsyncLifetime
{
    private readonly DatabaseFixture<ReviewRepositoryTests> _fixture;
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    private const int OwnerId = 1;
    private const int BorrowerId = 2;
    private const int ItemId = 1;

    public ReviewRepositoryTests(DatabaseFixture<ReviewRepositoryTests> fixture)
    {
        _fixture = fixture;
        _contextFactory = fixture.ContextFactory;
    }

    public async Task InitializeAsync() => await _fixture.ResetRentalsAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private ReviewRepository CreateSut() => new(_contextFactory);

    private async Task<Rental> CreateCompletedRentalAsync()
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
        return rental;
    }

    [Fact]
    public async Task CreateReviewAsync_ReturnsHydratedReview()
    {
        var rental = await CreateCompletedRentalAsync();

        var review = await CreateSut().CreateReviewAsync(rental.Id, ItemId, BorrowerId, 4, "Good!");

        Assert.Equal(rental.Id, review.RentalId);
        Assert.Equal(ItemId, review.ItemId);
        Assert.Equal(BorrowerId, review.ReviewerId);
        Assert.Equal(4, review.Rating);
        Assert.Equal("Good!", review.Comment);
        Assert.NotNull(review.Reviewer);
        Assert.NotNull(review.Item);
        Assert.NotNull(review.Rental);
    }

    [Fact]
    public async Task HasReviewForRentalAsync_BeforeCreation_ReturnsFalse()
    {
        var rental = await CreateCompletedRentalAsync();

        var result = await CreateSut().HasReviewForRentalAsync(rental.Id);

        Assert.False(result);
    }

    [Fact]
    public async Task HasReviewForRentalAsync_AfterCreation_ReturnsTrue()
    {
        var rental = await CreateCompletedRentalAsync();
        await CreateSut().CreateReviewAsync(rental.Id, ItemId, BorrowerId, 3, null);

        var result = await CreateSut().HasReviewForRentalAsync(rental.Id);

        Assert.True(result);
    }

    [Fact]
    public async Task GetItemReviewsPagedAsync_ReturnsCorrectPageAndTotal()
    {
        var rental = await CreateCompletedRentalAsync();
        await CreateSut().CreateReviewAsync(rental.Id, ItemId, BorrowerId, 5, "Great");

        var (reviews, total) = await CreateSut().GetItemReviewsPagedAsync(ItemId, 1, 10);

        Assert.Equal(1, total);
        Assert.Single(reviews);
        Assert.Equal(5, reviews.First().Rating);
    }

    [Fact]
    public async Task GetItemReviewsPagedAsync_WrongItemId_ReturnsEmpty()
    {
        var rental = await CreateCompletedRentalAsync();
        await CreateSut().CreateReviewAsync(rental.Id, ItemId, BorrowerId, 5, null);

        var (reviews, total) = await CreateSut().GetItemReviewsPagedAsync(999, 1, 10);

        Assert.Equal(0, total);
        Assert.Empty(reviews);
    }

    [Fact]
    public async Task GetItemReviewsPagedAsync_PaginatesCorrectly()
    {
        await using var context = _contextFactory.CreateDbContext();
        var r1 = new Rental
        {
            ItemId = ItemId,
            OwnerId = OwnerId,
            BorrowerId = BorrowerId,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-20)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-15)),
            Status = RentalStatus.Completed,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        var r2 = new Rental
        {
            ItemId = ItemId,
            OwnerId = OwnerId,
            BorrowerId = BorrowerId,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-14)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-10)),
            Status = RentalStatus.Completed,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        context.Rentals.AddRange(r1, r2);
        await context.SaveChangesAsync();
        await CreateSut().CreateReviewAsync(r1.Id, ItemId, BorrowerId, 5, "First");
        await CreateSut().CreateReviewAsync(r2.Id, ItemId, BorrowerId, 3, "Second");

        var (page1, total) = await CreateSut().GetItemReviewsPagedAsync(ItemId, 1, 1);
        var (page2, _) = await CreateSut().GetItemReviewsPagedAsync(ItemId, 2, 1);

        Assert.Equal(2, total);
        Assert.Single(page1);
        Assert.Single(page2);
    }

    [Fact]
    public async Task GetUserReviewsPagedAsync_ReturnsCorrectPageAndTotal()
    {
        var rental = await CreateCompletedRentalAsync();
        await CreateSut().CreateReviewAsync(rental.Id, ItemId, BorrowerId, 4, "Nice");

        var (reviews, total) = await CreateSut().GetUserReviewsPagedAsync(OwnerId, 1, 10);

        Assert.Equal(1, total);
        Assert.Single(reviews);
    }
}
