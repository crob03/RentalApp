using System.Net;
using System.Net.Http.Json;
using NSubstitute;
using RentalApp.Contracts.Requests;
using RentalApp.Http;
using RentalApp.Services;

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
}
