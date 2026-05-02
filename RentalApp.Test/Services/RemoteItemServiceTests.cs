using System.Net;
using System.Net.Http.Json;
using NSubstitute;
using RentalApp.Contracts.Requests;
using RentalApp.Http;
using RentalApp.Services;

namespace RentalApp.Test.Services;

public class RemoteItemServiceTests
{
    private readonly IApiClient _apiClient = Substitute.For<IApiClient>();

    private RemoteItemService CreateSut() => new(_apiClient);

    [Fact]
    public async Task GetItemsAsync_SuccessResponse_ReturnsItems()
    {
        _apiClient
            .GetAsync(Arg.Is<string>(s => s.StartsWith("items?")))
            .Returns(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(
                        new
                        {
                            items = Array.Empty<object>(),
                            totalItems = 0,
                            page = 1,
                            pageSize = 20,
                            totalPages = 0,
                        }
                    ),
                }
            );

        var result = await CreateSut().GetItemsAsync(new GetItemsRequest(1, 20, null, null));

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetItemsAsync_WithCategoryAndSearch_BuildsCorrectQuery()
    {
        string? capturedUrl = null;
        _apiClient
            .GetAsync(Arg.Do<string>(u => capturedUrl = u))
            .Returns(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(
                        new
                        {
                            items = Array.Empty<object>(),
                            totalItems = 0,
                            page = 1,
                            pageSize = 20,
                            totalPages = 0,
                        }
                    ),
                }
            );

        await CreateSut().GetItemsAsync(new GetItemsRequest(1, 20, "tools", "drill"));

        Assert.Contains("category=tools", capturedUrl);
        Assert.Contains("search=drill", capturedUrl);
    }

    [Fact]
    public async Task GetItemAsync_SuccessResponse_ReturnsItem()
    {
        _apiClient
            .GetAsync("items/5")
            .Returns(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(
                        new
                        {
                            id = 5,
                            title = "Drill",
                            description = "A drill",
                            dailyRate = 10.0m,
                            categoryId = 1,
                            categoryName = "Tools",
                            ownerId = 1,
                            ownerName = "Jane Doe",
                            ownerRating = (double?)null,
                            latitude = 55.0,
                            longitude = -3.0,
                            isAvailable = true,
                            averageRating = (double?)null,
                            totalReviews = 0,
                            createdAt = DateTime.UtcNow,
                            reviews = Array.Empty<object>(),
                        }
                    ),
                }
            );

        var result = await CreateSut().GetItemAsync(5);

        Assert.Equal(5, result.Id);
    }

    [Fact]
    public async Task GetCategoriesAsync_SuccessResponse_ReturnsCategories()
    {
        _apiClient
            .GetAsync("categories")
            .Returns(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new { categories = Array.Empty<object>() }),
                }
            );

        var result = await CreateSut().GetCategoriesAsync();

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetItemsAsync_ErrorResponse_ThrowsHttpRequestException()
    {
        _apiClient
            .GetAsync(Arg.Any<string>())
            .Returns(
                new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = JsonContent.Create(new { error = "Error", message = "Server error" }),
                }
            );

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            CreateSut().GetItemsAsync(new GetItemsRequest(1, 20, null, null))
        );
    }
}
