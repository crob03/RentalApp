using RentalApp.Contracts.Requests;
using RentalApp.Services.Rentals;

namespace RentalApp.Test.Services;

public class LocalRentalServiceTests
{
    private LocalRentalService CreateSut() => new();

    [Fact]
    public async Task GetIncomingRentalsAsync_ThrowsNotImplementedException()
    {
        await Assert.ThrowsAsync<NotImplementedException>(() =>
            CreateSut().GetIncomingRentalsAsync(new GetRentalsRequest())
        );
    }

    [Fact]
    public async Task GetOutgoingRentalsAsync_ThrowsNotImplementedException()
    {
        await Assert.ThrowsAsync<NotImplementedException>(() =>
            CreateSut().GetOutgoingRentalsAsync(new GetRentalsRequest())
        );
    }

    [Fact]
    public async Task GetRentalAsync_ThrowsNotImplementedException()
    {
        await Assert.ThrowsAsync<NotImplementedException>(() => CreateSut().GetRentalAsync(1));
    }

    [Fact]
    public async Task CreateRentalAsync_ThrowsNotImplementedException()
    {
        await Assert.ThrowsAsync<NotImplementedException>(() =>
            CreateSut()
                .CreateRentalAsync(
                    new CreateRentalRequest(
                        1,
                        DateOnly.FromDateTime(DateTime.Today),
                        DateOnly.FromDateTime(DateTime.Today.AddDays(1))
                    )
                )
        );
    }

    [Fact]
    public async Task UpdateRentalStatusAsync_ThrowsNotImplementedException()
    {
        await Assert.ThrowsAsync<NotImplementedException>(() =>
            CreateSut().UpdateRentalStatusAsync(1, new UpdateRentalStatusRequest("approved"))
        );
    }
}
