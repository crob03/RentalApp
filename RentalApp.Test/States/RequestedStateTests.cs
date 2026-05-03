using RentalApp.Database.Models;
using RentalApp.Database.States;

namespace RentalApp.Test.States;

public class RequestedStateTests
{
    private static Rental AnyRental() => new() { Status = RentalStatus.Requested };

    [Fact]
    public void Status_IsRequested() =>
        Assert.Equal(RentalStatus.Requested, new RequestedState().Status);

    [Fact]
    public async Task TransitionTo_Approved_ReturnsApprovedState()
    {
        var result = await new RequestedState().TransitionTo(RentalStatus.Approved, AnyRental());
        Assert.IsType<ApprovedState>(result);
    }

    [Fact]
    public async Task TransitionTo_Rejected_ReturnsRejectedState()
    {
        var result = await new RequestedState().TransitionTo(RentalStatus.Rejected, AnyRental());
        Assert.IsType<RejectedState>(result);
    }

    [Theory]
    [InlineData(RentalStatus.OutForRent)]
    [InlineData(RentalStatus.Returned)]
    [InlineData(RentalStatus.Completed)]
    [InlineData(RentalStatus.Overdue)]
    public async Task TransitionTo_InvalidTarget_Throws(RentalStatus target) =>
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new RequestedState().TransitionTo(target, AnyRental())
        );
}
