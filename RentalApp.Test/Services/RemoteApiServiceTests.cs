using System.Net;
using System.Net.Http.Json;
using NSubstitute;
using RentalApp.Contracts.Requests;
using RentalApp.Http;
using RentalApp.Services;

namespace RentalApp.Test.Services;

public class RemoteApiServiceTests
{
    private readonly IApiClient _apiClient = Substitute.For<IApiClient>();

    private RemoteApiService CreateSut() => new(_apiClient);

    // ── Login ──────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_SuccessResponse_ReturnsTokenResponse()
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

        var result = await sut.LoginAsync(new LoginRequest("jane@example.com", "Password1!"));

        Assert.Equal("abc123", result.Token);
    }

    [Fact]
    public async Task LoginAsync_ErrorResponse_ThrowsHttpRequestException()
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

        var act = () => sut.LoginAsync(new LoginRequest("jane@example.com", "wrong"));

        await Assert.ThrowsAsync<HttpRequestException>(act);
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

        var act = () => sut.LoginAsync(new LoginRequest("jane@example.com", "wrong"));

        var ex = await Assert.ThrowsAsync<HttpRequestException>(act);
        Assert.Equal("Invalid credentials", ex.Message);
    }

    // ── Register ───────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterAsync_SuccessResponse_Completes()
    {
        _apiClient
            .PostAsJsonAsync("auth/register", Arg.Any<object>())
            .Returns(
                new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = JsonContent.Create(
                        new
                        {
                            id = 1,
                            email = "jane@example.com",
                            firstName = "Jane",
                            lastName = "Doe",
                            createdAt = DateTime.UtcNow,
                        }
                    ),
                }
            );
        var sut = CreateSut();

        await sut.RegisterAsync(
            new RegisterRequest("Jane", "Doe", "jane@example.com", "Password1!")
        );

        await _apiClient.Received(1).PostAsJsonAsync("auth/register", Arg.Any<object>());
    }

    [Fact]
    public async Task RegisterAsync_ErrorResponse_ThrowsHttpRequestException()
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

        var act = () =>
            sut.RegisterAsync(new RegisterRequest("Jane", "Doe", "jane@example.com", "Password1!"));

        await Assert.ThrowsAsync<HttpRequestException>(act);
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

    // ── GetUserProfile ─────────────────────────────────────────────────

    [Fact]
    public async Task GetUserProfileAsync_SuccessResponse_ReturnsMappedUserWithReviews()
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

        var user = await sut.GetUserProfileAsync(1);

        Assert.Equal(1, user.Id);
        Assert.Equal("Jane", user.FirstName);
        Assert.Single(user.Reviews);
        Assert.Equal(5, user.Reviews[0].Rating);
        Assert.Equal("Great!", user.Reviews[0].Comment);
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

        var response = await sut.GetItemsAsync(new GetItemsRequest());

        Assert.Single(response.Items);
        Assert.Equal("Drill", response.Items[0].Title);
        Assert.Equal("Tools", response.Items[0].Category);
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

        var response = await sut.GetNearbyItemsAsync(new GetNearbyItemsRequest(55.95, -3.19));

        Assert.Single(response.Items);
        Assert.Equal(0.4, response.Items[0].Distance);
        Assert.Equal(55.9533, response.Items[0].Latitude);
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
        Assert.Single(item.Reviews);
        Assert.Equal(4, item.Reviews[0].Rating);
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

        var item = await sut.CreateItemAsync(
            new CreateItemRequest("New Drill", null, 12.0, 1, 55.9533, -3.1883)
        );

        Assert.Equal(5, item.Id);
        Assert.Equal("New Drill", item.Title);
    }

    // ── UpdateItemAsync ────────────────────────────────────────────────

    [Fact]
    public async Task UpdateItemAsync_SuccessResponse_ReturnsMappedItem()
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
        var sut = CreateSut();

        var item = await sut.UpdateItemAsync(
            1,
            new UpdateItemRequest("Updated", null, null, false)
        );

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

        var response = await sut.GetCategoriesAsync();

        Assert.Single(response.Categories);
        Assert.Equal("Tools", response.Categories[0].Name);
    }

    // ── GetIncomingRentalsAsync ────────────────────────────────────────

    [Fact]
    public async Task GetIncomingRentalsAsync_SuccessResponse_ReturnsMappedRentals()
    {
        _apiClient
            .GetAsync("rentals/incoming")
            .Returns(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(
                        new
                        {
                            rentals = new[]
                            {
                                new
                                {
                                    id = 1,
                                    itemId = 2,
                                    itemTitle = "Drill",
                                    borrowerId = 3,
                                    borrowerName = "Bob Smith",
                                    ownerId = 1,
                                    ownerName = "Jane Doe",
                                    startDate = new DateOnly(2026, 3, 1),
                                    endDate = new DateOnly(2026, 3, 5),
                                    status = "pending",
                                    totalPrice = 40.0,
                                    createdAt = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                                },
                            },
                            totalRentals = 1,
                        }
                    ),
                }
            );
        var sut = CreateSut();

        var response = await sut.GetIncomingRentalsAsync(new GetRentalsRequest());

        Assert.Single(response.Rentals);
        Assert.Equal("Drill", response.Rentals[0].ItemTitle);
        Assert.Equal("pending", response.Rentals[0].Status);
    }

    [Fact]
    public async Task GetIncomingRentalsAsync_WithStatusFilter_IncludesStatusInQuery()
    {
        _apiClient
            .GetAsync("rentals/incoming?status=active")
            .Returns(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new { rentals = Array.Empty<object>(), totalRentals = 0 }),
                }
            );
        var sut = CreateSut();

        await sut.GetIncomingRentalsAsync(new GetRentalsRequest("active"));

        await _apiClient.Received(1).GetAsync("rentals/incoming?status=active");
    }

    // ── GetOutgoingRentalsAsync ────────────────────────────────────────

    [Fact]
    public async Task GetOutgoingRentalsAsync_SuccessResponse_ReturnsMappedRentals()
    {
        _apiClient
            .GetAsync("rentals/outgoing")
            .Returns(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(
                        new
                        {
                            rentals = new[]
                            {
                                new
                                {
                                    id = 2,
                                    itemId = 5,
                                    itemTitle = "Ladder",
                                    borrowerId = 7,
                                    borrowerName = "Alice",
                                    ownerId = 1,
                                    ownerName = "Jane Doe",
                                    startDate = new DateOnly(2026, 4, 1),
                                    endDate = new DateOnly(2026, 4, 3),
                                    status = "active",
                                    totalPrice = 20.0,
                                    createdAt = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                                },
                            },
                            totalRentals = 1,
                        }
                    ),
                }
            );
        var sut = CreateSut();

        var response = await sut.GetOutgoingRentalsAsync(new GetRentalsRequest());

        Assert.Single(response.Rentals);
        Assert.Equal("Ladder", response.Rentals[0].ItemTitle);
        Assert.Equal("active", response.Rentals[0].Status);
    }

    // ── GetRentalAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task GetRentalAsync_SuccessResponse_ReturnsMappedDetail()
    {
        _apiClient
            .GetAsync("rentals/1")
            .Returns(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(
                        new
                        {
                            id = 1,
                            itemId = 2,
                            itemTitle = "Drill",
                            itemDescription = (string?)null,
                            borrowerId = 3,
                            borrowerName = "Bob Smith",
                            ownerId = 1,
                            ownerName = "Jane Doe",
                            startDate = new DateOnly(2026, 3, 1),
                            endDate = new DateOnly(2026, 3, 5),
                            status = "active",
                            totalPrice = 40.0,
                            requestedAt = new DateTime(2026, 2, 15, 0, 0, 0, DateTimeKind.Utc),
                        }
                    ),
                }
            );
        var sut = CreateSut();

        var rental = await sut.GetRentalAsync(1);

        Assert.Equal(1, rental.Id);
        Assert.Equal("Drill", rental.ItemTitle);
        Assert.Equal("active", rental.Status);
        Assert.Equal(40.0, rental.TotalPrice);
    }

    [Fact]
    public async Task GetRentalAsync_ErrorResponse_ThrowsHttpRequestException()
    {
        _apiClient
            .GetAsync("rentals/99")
            .Returns(
                new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = JsonContent.Create(new { error = "NotFound", message = "Rental not found" }),
                }
            );
        var sut = CreateSut();

        var act = () => sut.GetRentalAsync(99);

        var ex = await Assert.ThrowsAsync<HttpRequestException>(act);
        Assert.Equal("Rental not found", ex.Message);
    }

    // ── CreateRentalAsync ──────────────────────────────────────────────

    [Fact]
    public async Task CreateRentalAsync_SuccessResponse_ReturnsMappedRental()
    {
        _apiClient
            .PostAsJsonAsync("rentals", Arg.Any<object>())
            .Returns(
                new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = JsonContent.Create(
                        new
                        {
                            id = 10,
                            itemId = 2,
                            itemTitle = "Drill",
                            borrowerId = 3,
                            borrowerName = "Bob Smith",
                            ownerId = 1,
                            ownerName = "Jane Doe",
                            startDate = new DateOnly(2026, 5, 1),
                            endDate = new DateOnly(2026, 5, 3),
                            status = "pending",
                            totalPrice = 20.0,
                            createdAt = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
                        }
                    ),
                }
            );
        var sut = CreateSut();

        var rental = await sut.CreateRentalAsync(
            new CreateRentalRequest(2, new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 3))
        );

        Assert.Equal(10, rental.Id);
        Assert.Equal("pending", rental.Status);
        Assert.Equal(20.0, rental.TotalPrice);
    }

    // ── UpdateRentalStatusAsync ────────────────────────────────────────

    [Fact]
    public async Task UpdateRentalStatusAsync_SuccessResponse_ReturnsUpdatedStatus()
    {
        _apiClient
            .PutAsJsonAsync("rentals/1/status", Arg.Any<object>())
            .Returns(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(
                        new
                        {
                            id = 1,
                            status = "approved",
                            updatedAt = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
                        }
                    ),
                }
            );
        var sut = CreateSut();

        var result = await sut.UpdateRentalStatusAsync(1, new UpdateRentalStatusRequest("approved"));

        Assert.Equal(1, result.Id);
        Assert.Equal("approved", result.Status);
    }

    // ── GetItemReviewsAsync ────────────────────────────────────────────

    [Fact]
    public async Task GetItemReviewsAsync_SuccessResponse_ReturnsMappedReviews()
    {
        _apiClient
            .GetAsync(Arg.Is<string>(s => s.StartsWith("items/1/reviews")))
            .Returns(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(
                        new
                        {
                            reviews = new[]
                            {
                                new
                                {
                                    id = 5,
                                    rating = 4,
                                    comment = "Good condition",
                                    reviewerName = "Bob",
                                    createdAt = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                                },
                            },
                            averageRating = (double?)4.0,
                            totalReviews = 1,
                            page = 1,
                            pageSize = 10,
                            totalPages = 1,
                        }
                    ),
                }
            );
        var sut = CreateSut();

        var response = await sut.GetItemReviewsAsync(1, new GetReviewsRequest());

        Assert.Single(response.Reviews);
        Assert.Equal(4, response.Reviews[0].Rating);
        Assert.Equal("Good condition", response.Reviews[0].Comment);
        Assert.Equal(4.0, response.AverageRating);
    }

    // ── GetUserReviewsAsync ────────────────────────────────────────────

    [Fact]
    public async Task GetUserReviewsAsync_SuccessResponse_ReturnsMappedReviews()
    {
        _apiClient
            .GetAsync(Arg.Is<string>(s => s.StartsWith("users/1/reviews")))
            .Returns(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(
                        new
                        {
                            reviews = new[]
                            {
                                new
                                {
                                    id = 3,
                                    rating = 5,
                                    comment = "Excellent!",
                                    reviewerName = "Alice",
                                    createdAt = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                                },
                            },
                            averageRating = (double?)5.0,
                            totalReviews = 1,
                            page = 1,
                            pageSize = 10,
                            totalPages = 1,
                        }
                    ),
                }
            );
        var sut = CreateSut();

        var response = await sut.GetUserReviewsAsync(1, new GetReviewsRequest());

        Assert.Single(response.Reviews);
        Assert.Equal(5, response.Reviews[0].Rating);
        Assert.Equal("Excellent!", response.Reviews[0].Comment);
    }

    // ── CreateReviewAsync ──────────────────────────────────────────────

    [Fact]
    public async Task CreateReviewAsync_SuccessResponse_ReturnsMappedReview()
    {
        _apiClient
            .PostAsJsonAsync("reviews", Arg.Any<object>())
            .Returns(
                new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = JsonContent.Create(
                        new
                        {
                            id = 7,
                            rentalId = 1,
                            reviewerId = 3,
                            reviewerName = "Bob Smith",
                            rating = 4,
                            comment = "Great item!",
                            createdAt = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
                        }
                    ),
                }
            );
        var sut = CreateSut();

        var review = await sut.CreateReviewAsync(new CreateReviewRequest(1, 4, "Great item!"));

        Assert.Equal(7, review.Id);
        Assert.Equal(4, review.Rating);
        Assert.Equal("Great item!", review.Comment);
    }

    [Fact]
    public async Task CreateReviewAsync_ErrorResponse_ThrowsHttpRequestException()
    {
        _apiClient
            .PostAsJsonAsync("reviews", Arg.Any<object>())
            .Returns(
                new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = JsonContent.Create(
                        new
                        {
                            error = "BadRequest",
                            message = "Review already submitted for this rental",
                        }
                    ),
                }
            );
        var sut = CreateSut();

        var act = () => sut.CreateReviewAsync(new CreateReviewRequest(1, 4, "Late"));

        var ex = await Assert.ThrowsAsync<HttpRequestException>(act);
        Assert.Equal("Review already submitted for this rental", ex.Message);
    }
}
