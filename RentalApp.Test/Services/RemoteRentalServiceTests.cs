using System.Net;
using System.Net.Http.Json;
using NSubstitute;
using RentalApp.Contracts.Requests;
using RentalApp.Http;
using RentalApp.Services.Rentals;

namespace RentalApp.Test.Services;

public class RemoteRentalServiceTests
{
    private readonly IApiClient _apiClient = Substitute.For<IApiClient>();

    private RemoteRentalService CreateSut() => new(_apiClient);

    [Fact]
    public async Task GetIncomingRentalsAsync_SuccessResponse_ReturnsRentals()
    {
        _apiClient
            .GetAsync(Arg.Is<string>(s => s.StartsWith("rentals/incoming")))
            .Returns(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(
                        new { rentals = Array.Empty<object>(), totalRentals = 0 }
                    ),
                }
            );

        var result = await CreateSut().GetIncomingRentalsAsync(new GetRentalsRequest());

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetIncomingRentalsAsync_WithStatusFilter_BuildsCorrectQuery()
    {
        string? capturedUrl = null;
        _apiClient
            .GetAsync(Arg.Do<string>(u => capturedUrl = u))
            .Returns(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(
                        new { rentals = Array.Empty<object>(), totalRentals = 0 }
                    ),
                }
            );

        await CreateSut().GetIncomingRentalsAsync(new GetRentalsRequest(Status: "active"));

        Assert.Equal("rentals/incoming?status=active", capturedUrl);
    }

    [Fact]
    public async Task GetOutgoingRentalsAsync_SuccessResponse_ReturnsRentals()
    {
        _apiClient
            .GetAsync(Arg.Is<string>(s => s.StartsWith("rentals/outgoing")))
            .Returns(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(
                        new { rentals = Array.Empty<object>(), totalRentals = 0 }
                    ),
                }
            );

        var result = await CreateSut().GetOutgoingRentalsAsync(new GetRentalsRequest());

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetOutgoingRentalsAsync_WithStatusFilter_BuildsCorrectQuery()
    {
        string? capturedUrl = null;
        _apiClient
            .GetAsync(Arg.Do<string>(u => capturedUrl = u))
            .Returns(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(
                        new { rentals = Array.Empty<object>(), totalRentals = 0 }
                    ),
                }
            );

        await CreateSut().GetOutgoingRentalsAsync(new GetRentalsRequest(Status: "pending"));

        Assert.Equal("rentals/outgoing?status=pending", capturedUrl);
    }

    [Fact]
    public async Task GetRentalAsync_SuccessResponse_ReturnsRental()
    {
        _apiClient
            .GetAsync("rentals/7")
            .Returns(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(
                        new
                        {
                            id = 7,
                            itemId = 1,
                            itemTitle = "Test Drill",
                            itemDescription = "A drill",
                            borrowerId = 2,
                            borrowerName = "Alice Smith",
                            ownerId = 1,
                            ownerName = "Test User",
                            startDate = "2026-06-01",
                            endDate = "2026-06-07",
                            status = "pending",
                            totalPrice = 70.0,
                            requestedAt = DateTime.UtcNow,
                        }
                    ),
                }
            );

        var result = await CreateSut().GetRentalAsync(7);

        Assert.Equal(7, result.Id);
        Assert.Equal("pending", result.Status);
    }

    [Fact]
    public async Task GetRentalAsync_ErrorResponse_ThrowsHttpRequestException()
    {
        _apiClient
            .GetAsync("rentals/99")
            .Returns(
                new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = JsonContent.Create(new { error = "NotFound", message = "Not found" }),
                }
            );

        await Assert.ThrowsAsync<HttpRequestException>(() => CreateSut().GetRentalAsync(99));
    }

    [Fact]
    public async Task CreateRentalAsync_SuccessResponse_ReturnsRentalSummary()
    {
        _apiClient
            .PostAsJsonAsync(Arg.Is<string>(s => s == "rentals"), Arg.Any<object>())
            .Returns(
                new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = JsonContent.Create(
                        new
                        {
                            id = 12,
                            itemId = 1,
                            itemTitle = "Test Drill",
                            borrowerId = 2,
                            borrowerName = "Alice Smith",
                            ownerId = 1,
                            ownerName = "Test User",
                            startDate = "2026-06-01",
                            endDate = "2026-06-03",
                            status = "pending",
                            totalPrice = 20.0,
                            createdAt = DateTime.UtcNow,
                        }
                    ),
                }
            );

        var result = await CreateSut()
            .CreateRentalAsync(
                new CreateRentalRequest(
                    ItemId: 1,
                    StartDate: new DateOnly(2026, 6, 1),
                    EndDate: new DateOnly(2026, 6, 3)
                )
            );

        Assert.Equal(12, result.Id);
        Assert.Equal("pending", result.Status);
    }

    [Fact]
    public async Task UpdateRentalStatusAsync_SuccessResponse_ReturnsUpdatedStatus()
    {
        _apiClient
            .PatchAsJsonAsync(Arg.Is<string>(s => s == "rentals/7/status"), Arg.Any<object>())
            .Returns(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(
                        new
                        {
                            id = 7,
                            status = "approved",
                            updatedAt = DateTime.UtcNow,
                        }
                    ),
                }
            );

        var result = await CreateSut()
            .UpdateRentalStatusAsync(7, new UpdateRentalStatusRequest("approved"));

        Assert.Equal(7, result.Id);
        Assert.Equal("approved", result.Status);
    }
}
