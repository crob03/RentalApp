using RentalApp.Contracts.Requests;
using RentalApp.Services;

namespace RentalApp.Test.Services;

public class LocalReviewServiceTests
{
    private LocalReviewService CreateSut() => new();

    [Fact]
    public async Task GetItemReviewsAsync_ThrowsNotImplementedException()
    {
        await Assert.ThrowsAsync<NotImplementedException>(() =>
            CreateSut().GetItemReviewsAsync(1, new GetReviewsRequest(1, 20))
        );
    }

    [Fact]
    public async Task GetUserReviewsAsync_ThrowsNotImplementedException()
    {
        await Assert.ThrowsAsync<NotImplementedException>(() =>
            CreateSut().GetUserReviewsAsync(1, new GetReviewsRequest(1, 20))
        );
    }

    [Fact]
    public async Task CreateReviewAsync_ThrowsNotImplementedException()
    {
        await Assert.ThrowsAsync<NotImplementedException>(() =>
            CreateSut().CreateReviewAsync(new CreateReviewRequest(1, 5, "Great"))
        );
    }
}
