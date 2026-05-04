using NSubstitute;
using NSubstitute.ExceptionExtensions;
using RentalApp.Contracts.Responses;
using RentalApp.Services.Auth;
using RentalApp.Services.Navigation;
using RentalApp.ViewModels;

namespace RentalApp.Test.ViewModels;

public class ReviewsViewModelTests
{
    private readonly INavigationService _nav = Substitute.For<INavigationService>();
    private readonly AuthTokenState _tokenState = new();
    private readonly ICredentialStore _credentialStore = Substitute.For<ICredentialStore>();

    private sealed class TestableViewModel(
        AuthTokenState tokenState,
        ICredentialStore credentialStore,
        INavigationService nav,
        Func<int, Task<ReviewsResponse>> fetchReviews
    ) : ReviewsViewModel(tokenState, credentialStore, nav)
    {
        protected override Task<ReviewsResponse> FetchReviewsAsync(int page) => fetchReviews(page);
    }

    private TestableViewModel CreateSut(Func<int, Task<ReviewsResponse>> fetchReviews) =>
        new(_tokenState, _credentialStore, _nav, fetchReviews);

    private static ReviewsResponse MakeResponse(
        int page = 1,
        int totalPages = 1,
        int total = 2,
        double? avg = 4.0,
        List<ReviewResponse>? reviews = null
    ) => new(reviews ?? [MakeReview(1), MakeReview(2)], avg, total, page, 10, totalPages);

    private static ReviewResponse MakeReview(int id) =>
        new(id, 4, "Great!", "Alice Smith", DateTime.UtcNow);

    // ── LoadReviewsCommand ─────────────────────────────────────────────

    [Fact]
    public async Task LoadReviewsCommand_PopulatesReviewsAverageRatingAndTotalReviews()
    {
        var sut = CreateSut(_ => Task.FromResult(MakeResponse(total: 5, avg: 3.5)));

        await sut.LoadReviewsCommand.ExecuteAsync(null);

        Assert.Equal(2, sut.Reviews.Count);
        Assert.Equal(3.5, sut.AverageRating);
        Assert.Equal(5, sut.TotalReviews);
    }

    [Fact]
    public async Task LoadReviewsCommand_ResetsToPageOne()
    {
        var capturedPages = new List<int>();
        var sut = CreateSut(page =>
        {
            capturedPages.Add(page);
            return Task.FromResult(MakeResponse(page: page, totalPages: 3));
        });
        await sut.LoadReviewsCommand.ExecuteAsync(null); // loads page 1 + more available
        await sut.LoadMoreReviewsCommand.ExecuteAsync(null); // advances to page 2
        capturedPages.Clear();

        await sut.LoadReviewsCommand.ExecuteAsync(null); // should reset to page 1

        Assert.Equal(1, sut.CurrentReviewPage);
        Assert.Equal([1], capturedPages);
    }

    [Fact]
    public async Task LoadReviewsCommand_SetsHasMoreReviewPagesTrue_WhenPageLessThanTotalPages()
    {
        var sut = CreateSut(_ => Task.FromResult(MakeResponse(page: 1, totalPages: 3)));

        await sut.LoadReviewsCommand.ExecuteAsync(null);

        Assert.True(sut.HasMoreReviewPages);
    }

    [Fact]
    public async Task LoadReviewsCommand_SetsHasMoreReviewPagesFalse_WhenOnLastPage()
    {
        var sut = CreateSut(_ => Task.FromResult(MakeResponse(page: 1, totalPages: 1)));

        await sut.LoadReviewsCommand.ExecuteAsync(null);

        Assert.False(sut.HasMoreReviewPages);
    }

    [Fact]
    public async Task LoadReviewsCommand_ServiceThrows_SetsError()
    {
        var sut = CreateSut(_ =>
            Task.FromException<ReviewsResponse>(new Exception("fetch failed"))
        );

        await sut.LoadReviewsCommand.ExecuteAsync(null);

        Assert.True(sut.HasError);
        Assert.Equal("fetch failed", sut.ErrorMessage);
    }

    [Fact]
    public async Task LoadReviewsCommand_IsLoadingReviewsFalse_AfterCompletion()
    {
        var sut = CreateSut(_ => Task.FromResult(MakeResponse()));

        await sut.LoadReviewsCommand.ExecuteAsync(null);

        Assert.False(sut.IsLoadingReviews);
    }

    // ── LoadMoreReviewsCommand ─────────────────────────────────────────

    [Fact]
    public async Task LoadMoreReviewsCommand_DoesNothing_WhenNoMorePages()
    {
        var fetchCount = 0;
        var sut = CreateSut(_ =>
        {
            fetchCount++;
            return Task.FromResult(MakeResponse(page: 1, totalPages: 1));
        });
        await sut.LoadReviewsCommand.ExecuteAsync(null); // HasMoreReviewPages = false

        await sut.LoadMoreReviewsCommand.ExecuteAsync(null);

        Assert.Equal(1, fetchCount); // only the initial load
    }

    [Fact]
    public async Task LoadMoreReviewsCommand_AppendsReviewsAndIncrementsPage()
    {
        var sut = CreateSut(page =>
            Task.FromResult(
                MakeResponse(page: page, totalPages: 2, reviews: [MakeReview(page * 10)])
            )
        );
        await sut.LoadReviewsCommand.ExecuteAsync(null); // page 1 → 1 review

        await sut.LoadMoreReviewsCommand.ExecuteAsync(null); // page 2 → appended

        Assert.Equal(2, sut.Reviews.Count);
        Assert.Equal(2, sut.CurrentReviewPage);
    }

    [Fact]
    public async Task LoadMoreReviewsCommand_RollsBackCurrentReviewPage_OnFailure()
    {
        var callCount = 0;
        var sut = CreateSut(page =>
        {
            callCount++;
            if (callCount == 1)
                return Task.FromResult(MakeResponse(page: 1, totalPages: 3));
            return Task.FromException<ReviewsResponse>(new Exception("timeout"));
        });
        await sut.LoadReviewsCommand.ExecuteAsync(null); // page 1

        await sut.LoadMoreReviewsCommand.ExecuteAsync(null); // page 2 attempt fails

        Assert.Equal(1, sut.CurrentReviewPage);
        Assert.True(sut.HasError);
    }

    // ── HasAverageRating ───────────────────────────────────────────────

    [Fact]
    public async Task HasAverageRating_IsFalse_WhenNoReviews()
    {
        var sut = CreateSut(_ => Task.FromResult(MakeResponse(avg: null, total: 0, reviews: [])));

        await sut.LoadReviewsCommand.ExecuteAsync(null);

        Assert.False(sut.HasAverageRating);
        Assert.Null(sut.AverageRating);
    }

    [Fact]
    public async Task HasAverageRating_IsTrue_WhenReviewsPresent()
    {
        var sut = CreateSut(_ => Task.FromResult(MakeResponse(avg: 4.5)));

        await sut.LoadReviewsCommand.ExecuteAsync(null);

        Assert.True(sut.HasAverageRating);
        Assert.Equal(4.5, sut.AverageRating);
    }
}
