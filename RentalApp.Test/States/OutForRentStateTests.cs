using RentalApp.Database.Models;
using RentalApp.Database.States;

namespace RentalApp.Test.States;

public class OutForRentStateTests
{
    private static Rental AnyRental() => new() { Status = RentalStatus.OutForRent };

    [Fact]
    public void Status_IsOutForRent() =>
        Assert.Equal(RentalStatus.OutForRent, new OutForRentState().Status);

    [Fact]
    public async Task TransitionTo_Overdue_ReturnsOverdueState()
    {
        var result = await new OutForRentState().TransitionTo(RentalStatus.Overdue, AnyRental());
        Assert.IsType<OverdueState>(result);
    }

    [Fact]
    public async Task TransitionTo_Returned_ReturnsReturnedState()
    {
        var result = await new OutForRentState().TransitionTo(RentalStatus.Returned, AnyRental());
        Assert.IsType<ReturnedState>(result);
    }

    [Theory]
    [InlineData(RentalStatus.Requested)]
    [InlineData(RentalStatus.Approved)]
    [InlineData(RentalStatus.Rejected)]
    [InlineData(RentalStatus.Completed)]
    public async Task TransitionTo_InvalidTarget_Throws(RentalStatus target) =>
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new OutForRentState().TransitionTo(target, AnyRental())
        );
}
