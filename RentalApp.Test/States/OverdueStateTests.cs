using RentalApp.Database.Models;
using RentalApp.Database.States;

namespace RentalApp.Test.States;

public class OverdueStateTests
{
    private static Rental AnyRental() => new() { Status = RentalStatus.Overdue };

    [Fact]
    public void Status_IsOverdue() => Assert.Equal(RentalStatus.Overdue, new OverdueState().Status);

    [Fact]
    public async Task TransitionTo_Returned_ReturnsReturnedState()
    {
        var result = await new OverdueState().TransitionTo(RentalStatus.Returned, AnyRental());
        Assert.IsType<ReturnedState>(result);
    }

    [Theory]
    [InlineData(RentalStatus.Requested)]
    [InlineData(RentalStatus.Approved)]
    [InlineData(RentalStatus.Rejected)]
    [InlineData(RentalStatus.OutForRent)]
    [InlineData(RentalStatus.Completed)]
    public async Task TransitionTo_InvalidTarget_Throws(RentalStatus target) =>
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new OverdueState().TransitionTo(target, AnyRental())
        );
}
