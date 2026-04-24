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
}
