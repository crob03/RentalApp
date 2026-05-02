using System.Net;
using System.Net.Http.Json;
using NSubstitute;
using RentalApp.Contracts.Requests;
using RentalApp.Http;
using RentalApp.Services;

namespace RentalApp.Test.Services;

public class RemoteReviewServiceTests
{
    private readonly IApiClient _apiClient = Substitute.For<IApiClient>();

    private RemoteReviewService CreateSut() => new(_apiClient);

    [Fact]
    public async Task GetItemReviewsAsync_SuccessResponse_ReturnsReviews()
    {
        _apiClient
            .GetAsync(Arg.Is<string>(s => s.StartsWith("items/5/reviews")))
            .Returns(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { reviews = Array.Empty<object>(), totalReviews = 0, page = 1, pageSize = 20, totalPages = 0 }),
            });

        var result = await CreateSut().GetItemReviewsAsync(5, new GetReviewsRequest(1, 20));

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetUserReviewsAsync_SuccessResponse_ReturnsReviews()
    {
        _apiClient
            .GetAsync(Arg.Is<string>(s => s.StartsWith("users/3/reviews")))
            .Returns(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new { reviews = Array.Empty<object>(), totalReviews = 0, page = 1, pageSize = 20, totalPages = 0 }),
            });

        var result = await CreateSut().GetUserReviewsAsync(3, new GetReviewsRequest(1, 20));

        Assert.NotNull(result);
    }

    [Fact]
    public async Task CreateReviewAsync_ErrorResponse_ThrowsHttpRequestException()
    {
        _apiClient
            .PostAsJsonAsync("reviews", Arg.Any<object>())
            .Returns(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = JsonContent.Create(new { error = "Bad", message = "Invalid" }),
            });

        await Assert.ThrowsAsync<HttpRequestException>(
            () => CreateSut().CreateReviewAsync(new CreateReviewRequest(1, 5, "Great"))
        );
    }
}
