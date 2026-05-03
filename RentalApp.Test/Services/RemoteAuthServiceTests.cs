using System.Net;
using System.Net.Http.Json;
using NSubstitute;
using RentalApp.Contracts.Requests;
using RentalApp.Http;
using RentalApp.Services.Auth;

namespace RentalApp.Test.Services;

public class RemoteAuthServiceTests
{
    private readonly IApiClient _apiClient = Substitute.For<IApiClient>();

    private RemoteAuthService CreateSut() => new(_apiClient);

    [Fact]
    public async Task LoginAsync_SuccessResponse_ReturnsToken()
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

        var result = await CreateSut()
            .LoginAsync(new LoginRequest("jane@example.com", "Password1!"));

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

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            CreateSut().LoginAsync(new LoginRequest("jane@example.com", "wrong"))
        );
    }

    [Fact]
    public async Task RegisterAsync_SuccessResponse_ReturnsResponse()
    {
        _apiClient
            .PostAsJsonAsync("auth/register", Arg.Any<object>())
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
                            createdAt = DateTime.UtcNow,
                        }
                    ),
                }
            );

        var result = await CreateSut()
            .RegisterAsync(new RegisterRequest("Jane", "Doe", "jane@example.com", "Password1!"));

        Assert.Equal("jane@example.com", result.Email);
    }

    [Fact]
    public async Task RegisterAsync_ErrorResponse_ThrowsHttpRequestException()
    {
        _apiClient
            .PostAsJsonAsync("auth/register", Arg.Any<object>())
            .Returns(
                new HttpResponseMessage(HttpStatusCode.Conflict)
                {
                    Content = JsonContent.Create(
                        new { error = "Conflict", message = "Email already registered" }
                    ),
                }
            );

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            CreateSut()
                .RegisterAsync(new RegisterRequest("Jane", "Doe", "jane@example.com", "Password1!"))
        );
    }

    [Fact]
    public async Task GetCurrentUserAsync_SuccessResponse_ReturnsUser()
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
                            averageRating = (double?)null,
                            itemsListed = 0,
                            rentalsCompleted = 0,
                            createdAt = DateTime.UtcNow,
                        }
                    ),
                }
            );

        var result = await CreateSut().GetCurrentUserAsync();

        Assert.Equal("jane@example.com", result.Email);
    }

    [Fact]
    public async Task GetUserProfileAsync_SuccessResponse_ReturnsProfile()
    {
        _apiClient
            .GetAsync("users/42/profile")
            .Returns(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(
                        new
                        {
                            id = 42,
                            firstName = "Jane",
                            lastName = "Doe",
                            averageRating = (double?)null,
                            itemsListed = 0,
                            rentalsCompleted = 0,
                            reviews = Array.Empty<object>(),
                        }
                    ),
                }
            );

        var result = await CreateSut().GetUserProfileAsync(42);

        Assert.Equal(42, result.Id);
    }

    [Fact]
    public async Task GetCurrentUserAsync_ErrorResponse_ThrowsHttpRequestException()
    {
        _apiClient
            .GetAsync("users/me")
            .Returns(
                new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    Content = JsonContent.Create(
                        new { error = "Unauthorized", message = "Token expired" }
                    ),
                }
            );

        await Assert.ThrowsAsync<HttpRequestException>(() => CreateSut().GetCurrentUserAsync());
    }

    [Fact]
    public async Task GetUserProfileAsync_ErrorResponse_ThrowsHttpRequestException()
    {
        _apiClient
            .GetAsync("users/42/profile")
            .Returns(
                new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = JsonContent.Create(
                        new { error = "NotFound", message = "User not found" }
                    ),
                }
            );

        await Assert.ThrowsAsync<HttpRequestException>(() => CreateSut().GetUserProfileAsync(42));
    }
}
