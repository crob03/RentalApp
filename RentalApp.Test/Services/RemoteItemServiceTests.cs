using System.Net;
using System.Net.Http.Json;
using NSubstitute;
using RentalApp.Contracts.Requests;
using RentalApp.Http;
using RentalApp.Services.Items;

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

        Assert.Contains("page=1", capturedUrl);
        Assert.Contains("pageSize=20", capturedUrl);
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
    public async Task GetNearbyItemsAsync_SuccessResponse_ReturnsNearbyItems()
    {
        _apiClient
            .GetAsync(Arg.Is<string>(s => s.StartsWith("items/nearby?")))
            .Returns(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(
                        new
                        {
                            items = Array.Empty<object>(),
                            searchLocation = new { latitude = 55.9, longitude = -3.2 },
                            radius = 5.0,
                            totalResults = 0,
                        }
                    ),
                }
            );

        var result = await CreateSut()
            .GetNearbyItemsAsync(new GetNearbyItemsRequest(Lat: 55.9, Lon: -3.2, Radius: 5));

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetNearbyItemsAsync_WithCategory_BuildsCorrectQuery()
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
                            searchLocation = new { latitude = 55.9, longitude = -3.2 },
                            radius = 5.0,
                            totalResults = 0,
                        }
                    ),
                }
            );

        await CreateSut()
            .GetNearbyItemsAsync(
                new GetNearbyItemsRequest(Lat: 55.9, Lon: -3.2, Radius: 5, Category: "tools")
            );

        Assert.Contains("lat=55.9", capturedUrl);
        Assert.Contains("lon=-3.2", capturedUrl);
        Assert.Contains("radius=5", capturedUrl);
        Assert.Contains("category=tools", capturedUrl);
    }

    [Fact]
    public async Task CreateItemAsync_SuccessResponse_ReturnsCreatedItem()
    {
        _apiClient
            .PostAsJsonAsync(Arg.Is<string>(s => s == "items"), Arg.Any<object>())
            .Returns(
                new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = JsonContent.Create(
                        new
                        {
                            id = 10,
                            title = "New Drill",
                            description = "A new drill",
                            dailyRate = 12.0,
                            categoryId = 1,
                            category = "Tools",
                            ownerId = 1,
                            ownerName = "Jane Doe",
                            latitude = 55.9,
                            longitude = -3.2,
                            isAvailable = true,
                            createdAt = DateTime.UtcNow,
                        }
                    ),
                }
            );

        var result = await CreateSut()
            .CreateItemAsync(
                new CreateItemRequest("New Drill", "A new drill", 12.0, 1, 55.9, -3.2)
            );

        Assert.Equal(10, result.Id);
        Assert.Equal("New Drill", result.Title);
    }

    [Fact]
    public async Task CreateItemAsync_ErrorResponse_ThrowsHttpRequestException()
    {
        _apiClient
            .PostAsJsonAsync(Arg.Any<string>(), Arg.Any<object>())
            .Returns(
                new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = JsonContent.Create(
                        new { error = "BadRequest", message = "Invalid input" }
                    ),
                }
            );

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            CreateSut().CreateItemAsync(new CreateItemRequest("T", null, 1.0, 1, 55.0, -3.0))
        );
    }

    [Fact]
    public async Task UpdateItemAsync_SuccessResponse_ReturnsUpdatedItem()
    {
        _apiClient
            .PutAsJsonAsync(Arg.Is<string>(s => s == "items/3"), Arg.Any<object>())
            .Returns(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(
                        new
                        {
                            id = 3,
                            title = "Updated Drill",
                            description = "Updated",
                            dailyRate = 15.0,
                            isAvailable = false,
                        }
                    ),
                }
            );

        var result = await CreateSut()
            .UpdateItemAsync(3, new UpdateItemRequest("Updated Drill", "Updated", 15.0, false));

        Assert.Equal(3, result.Id);
        Assert.Equal("Updated Drill", result.Title);
        Assert.False(result.IsAvailable);
    }

    [Fact]
    public async Task UpdateItemAsync_ErrorResponse_ThrowsHttpRequestException()
    {
        _apiClient
            .PutAsJsonAsync(Arg.Any<string>(), Arg.Any<object>())
            .Returns(
                new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = JsonContent.Create(
                        new { error = "NotFound", message = "Item not found" }
                    ),
                }
            );

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            CreateSut().UpdateItemAsync(999, new UpdateItemRequest(null, null, null, null))
        );
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
