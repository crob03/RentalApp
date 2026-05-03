using RentalApp.Database.Models;
using RentalApp.Database.States;

namespace RentalApp.Test.States;

public class ReturnedStateTests
{
    private static Rental AnyRental() => new() { Status = RentalStatus.Returned };

    [Fact]
    public void Status_IsReturned() =>
        Assert.Equal(RentalStatus.Returned, new ReturnedState().Status);

    [Fact]
    public async Task TransitionTo_Completed_ReturnsCompletedState()
    {
        var result = await new ReturnedState().TransitionTo(RentalStatus.Completed, AnyRental());
        Assert.IsType<CompletedState>(result);
    }

    [Theory]
    [InlineData(RentalStatus.Requested)]
    [InlineData(RentalStatus.Approved)]
    [InlineData(RentalStatus.Rejected)]
    [InlineData(RentalStatus.OutForRent)]
    [InlineData(RentalStatus.Overdue)]
    public async Task TransitionTo_InvalidTarget_Throws(RentalStatus target) =>
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new ReturnedState().TransitionTo(target, AnyRental())
        );
}
