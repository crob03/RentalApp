using System.Net;
using System.Net.Http.Json;
using NSubstitute;
using RentalApp.Http;
using RentalApp.Services;

namespace RentalApp.Test.Services;

public class RemoteApiServiceTests
{
    private readonly IApiClient _apiClient = Substitute.For<IApiClient>();
    private readonly AuthTokenState _tokenState = new();

    private RemoteApiService CreateSut() => new(_apiClient, _tokenState);

    // ── Login ──────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_SuccessResponse_SetsTokenOnState()
    {
        _apiClient
            .PostAsJsonAsync("auth/token", Arg.Any<object>())
            .Returns(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(
                        new
                        {
                            token = "abc123",
                            expiresAt = DateTime.UtcNow.AddHours(1),
                            userId = 1,
                        }
                    ),
                }
            );
        var sut = CreateSut();

        await sut.LoginAsync("jane@example.com", "Password1!");

        Assert.Equal("abc123", _tokenState.CurrentToken);
    }

    [Fact]
    public async Task LoginAsync_ErrorResponse_ThrowsUnauthorizedAccessException()
    {
        _apiClient
            .PostAsJsonAsync("auth/token", Arg.Any<object>())
            .Returns(
                new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    Content = JsonContent.Create(
                        new { error = "Unauthorized", message = "Invalid credentials" }
                    ),
                }
            );
        var sut = CreateSut();

        var act = () => sut.LoginAsync("jane@example.com", "wrong");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(act);
    }

    [Fact]
    public async Task LoginAsync_ErrorResponse_UsesApiErrorMessage()
    {
        _apiClient
            .PostAsJsonAsync("auth/token", Arg.Any<object>())
            .Returns(
                new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    Content = JsonContent.Create(
                        new { error = "Unauthorized", message = "Invalid credentials" }
                    ),
                }
            );
        var sut = CreateSut();

        var act = () => sut.LoginAsync("jane@example.com", "wrong");

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(act);
        Assert.Equal("Invalid credentials", ex.Message);
    }

    // ── Register ───────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterAsync_SuccessResponse_Completes()
    {
        _apiClient
            .PostAsJsonAsync("auth/register", Arg.Any<object>())
            .Returns(new HttpResponseMessage(HttpStatusCode.Created));
        var sut = CreateSut();

        await sut.RegisterAsync("Jane", "Doe", "jane@example.com", "Password1!");

        await _apiClient.Received(1).PostAsJsonAsync("auth/register", Arg.Any<object>());
    }

    [Fact]
    public async Task RegisterAsync_ErrorResponse_ThrowsInvalidOperationException()
    {
        _apiClient
            .PostAsJsonAsync("auth/register", Arg.Any<object>())
            .Returns(
                new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = JsonContent.Create(
                        new { error = "BadRequest", message = "Email already registered" }
                    ),
                }
            );
        var sut = CreateSut();

        var act = () => sut.RegisterAsync("Jane", "Doe", "jane@example.com", "Password1!");

        await Assert.ThrowsAsync<InvalidOperationException>(act);
    }

    // ── GetCurrentUser ─────────────────────────────────────────────────

    [Fact]
    public async Task GetCurrentUserAsync_SuccessResponse_ReturnsMappedUser()
    {
        _apiClient
            .GetAsync("users/me")
            .Returns(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(
                        new
                        {
                            id = 1,
                            email = "jane@example.com",
                            firstName = "Jane",
                            lastName = "Doe",
                            averageRating = (double?)4.5,
                            itemsListed = 3,
                            rentalsCompleted = 7,
                            createdAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                        }
                    ),
                }
            );
        var sut = CreateSut();

        var user = await sut.GetCurrentUserAsync();

        Assert.Equal(1, user.Id);
        Assert.Equal("jane@example.com", user.Email);
        Assert.Equal("Jane", user.FirstName);
        Assert.Equal("Doe", user.LastName);
        Assert.Equal(4.5, user.AverageRating);
        Assert.Equal(3, user.ItemsListed);
        Assert.Equal(7, user.RentalsCompleted);
    }

    // ── GetUser ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetUserAsync_SuccessResponse_ReturnsMappedUserWithReviews()
    {
        _apiClient
            .GetAsync("users/1/profile")
            .Returns(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(
                        new
                        {
                            id = 1,
                            firstName = "Jane",
                            lastName = "Doe",
                            averageRating = (double?)4.0,
                            itemsListed = 2,
                            rentalsCompleted = 5,
                            reviews = new[]
                            {
                                new
                                {
                                    id = 10,
                                    rating = 5,
                                    comment = "Great!",
                                    reviewerName = "Bob",
                                    createdAt = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                                },
                            },
                        }
                    ),
                }
            );
        var sut = CreateSut();

        var user = await sut.GetUserAsync(1);

        Assert.Equal(1, user.Id);
        Assert.Equal("Jane", user.FirstName);
        Assert.Single(user.Reviews!);
        Assert.Equal(5, user.Reviews![0].Rating);
        Assert.Equal("Great!", user.Reviews[0].Comment);
    }

    // ── Logout ─────────────────────────────────────────────────────────

    [Fact]
    public async Task LogoutAsync_ClearsTokenState()
    {
        _tokenState.CurrentToken = "abc123";
        var sut = CreateSut();

        await sut.LogoutAsync();

        Assert.Null(_tokenState.CurrentToken);
    }

    // ── GetItemsAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetItemsAsync_SuccessResponse_ReturnsMappedItems()
    {
        _apiClient
            .GetAsync(Arg.Is<string>(s => s.StartsWith("items")))
            .Returns(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(
                        new
                        {
                            items = new[]
                            {
                                new
                                {
                                    id = 1,
                                    title = "Drill",
                                    description = (string?)null,
                                    dailyRate = 10.0,
                                    categoryId = 1,
                                    category = "Tools",
                                    ownerId = 1,
                                    ownerName = "Jane Doe",
                                    ownerRating = (double?)null,
                                    isAvailable = true,
                                    averageRating = (double?)null,
                                    createdAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                                },
                            },
                            totalItems = 1,
                            page = 1,
                            pageSize = 20,
                            totalPages = 1,
                        }
                    ),
                }
            );
        var sut = CreateSut();

        var items = await sut.GetItemsAsync();

        Assert.Single(items);
        Assert.Equal("Drill", items[0].Title);
        Assert.Equal("Tools", items[0].Category);
        Assert.Null(items[0].Latitude);
    }

    // ── GetNearbyItemsAsync ────────────────────────────────────────────

    [Fact]
    public async Task GetNearbyItemsAsync_SuccessResponse_ReturnsMappedItemsWithDistance()
    {
        _apiClient
            .GetAsync(Arg.Is<string>(s => s.StartsWith("items/nearby")))
            .Returns(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(
                        new
                        {
                            items = new[]
                            {
                                new
                                {
                                    id = 1,
                                    title = "Drill",
                                    description = (string?)null,
                                    dailyRate = 10.0,
                                    categoryId = 1,
                                    category = "Tools",
                                    ownerId = 1,
                                    ownerName = "Jane Doe",
                                    latitude = 55.9533,
                                    longitude = -3.1883,
                                    distance = 0.4,
                                    isAvailable = true,
                                    averageRating = (double?)null,
                                },
                            },
                            searchLocation = new { latitude = 55.95, longitude = -3.19 },
                            radius = 5.0,
                            totalResults = 1,
                        }
                    ),
                }
            );
        var sut = CreateSut();

        var items = await sut.GetNearbyItemsAsync(55.95, -3.19);

        Assert.Single(items);
        Assert.Equal(0.4, items[0].Distance);
        Assert.Equal(55.9533, items[0].Latitude);
    }

    // ── GetItemAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetItemAsync_SuccessResponse_ReturnsMappedItemWithReviews()
    {
        _apiClient
            .GetAsync("items/1")
            .Returns(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(
                        new
                        {
                            id = 1,
                            title = "Drill",
                            description = (string?)null,
                            dailyRate = 10.0,
                            categoryId = 1,
                            category = "Tools",
                            ownerId = 1,
                            ownerName = "Jane Doe",
                            ownerRating = (double?)4.5,
                            latitude = (double?)55.9533,
                            longitude = (double?)-3.1883,
                            isAvailable = true,
                            averageRating = (double?)4.0,
                            totalReviews = 1,
                            createdAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                            reviews = new[]
                            {
                                new
                                {
                                    id = 10,
                                    reviewerId = 2,
                                    reviewerName = "Bob",
                                    rating = 4,
                                    comment = "Good",
                                    createdAt = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                                },
                            },
                        }
                    ),
                }
            );
        var sut = CreateSut();

        var item = await sut.GetItemAsync(1);

        Assert.Equal(1, item.Id);
        Assert.Equal(4.5, item.OwnerRating);
        Assert.Single(item.Reviews!);
        Assert.Equal(4, item.Reviews![0].Rating);
    }

    // ── CreateItemAsync ────────────────────────────────────────────────

    [Fact]
    public async Task CreateItemAsync_SuccessResponse_ReturnsMappedItem()
    {
        _apiClient
            .PostAsJsonAsync("items", Arg.Any<object>())
            .Returns(
                new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = JsonContent.Create(
                        new
                        {
                            id = 5,
                            title = "New Drill",
                            description = (string?)null,
                            dailyRate = 12.0,
                            categoryId = 1,
                            category = "Tools",
                            ownerId = 1,
                            ownerName = "Jane Doe",
                            latitude = 55.9533,
                            longitude = -3.1883,
                            isAvailable = true,
                            createdAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                        }
                    ),
                }
            );
        var sut = CreateSut();

        var item = await sut.CreateItemAsync("New Drill", null, 12.0, 1, 55.9533, -3.1883);

        Assert.Equal(5, item.Id);
        Assert.Equal("New Drill", item.Title);
    }

    // ── UpdateItemAsync ────────────────────────────────────────────────

    [Fact]
    public async Task UpdateItemAsync_SuccessResponse_FetchesAndReturnsFullItem()
    {
        _apiClient
            .PutAsJsonAsync("items/1", Arg.Any<object>())
            .Returns(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(
                        new
                        {
                            id = 1,
                            title = "Updated",
                            description = (string?)null,
                            dailyRate = 10.0,
                            isAvailable = false,
                        }
                    ),
                }
            );
        _apiClient
            .GetAsync("items/1")
            .Returns(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(
                        new
                        {
                            id = 1,
                            title = "Updated",
                            description = (string?)null,
                            dailyRate = 10.0,
                            categoryId = 1,
                            category = "Tools",
                            ownerId = 1,
                            ownerName = "Jane Doe",
                            ownerRating = (double?)null,
                            latitude = (double?)55.9533,
                            longitude = (double?)-3.1883,
                            isAvailable = false,
                            averageRating = (double?)null,
                            totalReviews = 0,
                            createdAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                            reviews = Array.Empty<object>(),
                        }
                    ),
                }
            );
        var sut = CreateSut();

        var item = await sut.UpdateItemAsync(1, "Updated", null, null, false);

        Assert.Equal("Updated", item.Title);
        Assert.False(item.IsAvailable);
    }

    // ── GetCategoriesAsync ─────────────────────────────────────────────

    [Fact]
    public async Task GetCategoriesAsync_SuccessResponse_ReturnsMappedCategories()
    {
        _apiClient
            .GetAsync("categories")
            .Returns(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(
                        new
                        {
                            categories = new[]
                            {
                                new
                                {
                                    id = 1,
                                    name = "Tools",
                                    slug = "tools",
                                    itemCount = 5,
                                },
                            },
                        }
                    ),
                }
            );
        var sut = CreateSut();

        var categories = await sut.GetCategoriesAsync();

        Assert.Single(categories);
        Assert.Equal("Tools", categories[0].Name);
    }
}
