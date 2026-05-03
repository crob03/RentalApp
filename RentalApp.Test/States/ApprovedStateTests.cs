using RentalApp.Database.Models;
using RentalApp.Database.States;

namespace RentalApp.Test.States;

public class ApprovedStateTests
{
    private static Rental AnyRental() => new() { Status = RentalStatus.Approved };

    [Fact]
    public void Status_IsApproved() =>
        Assert.Equal(RentalStatus.Approved, new ApprovedState().Status);

    [Fact]
    public async Task TransitionTo_OutForRent_ReturnsOutForRentState()
    {
        var result = await new ApprovedState().TransitionTo(RentalStatus.OutForRent, AnyRental());
        Assert.IsType<OutForRentState>(result);
    }

    [Theory]
    [InlineData(RentalStatus.Requested)]
    [InlineData(RentalStatus.Rejected)]
    [InlineData(RentalStatus.Returned)]
    [InlineData(RentalStatus.Completed)]
    [InlineData(RentalStatus.Overdue)]
    public async Task TransitionTo_InvalidTarget_Throws(RentalStatus target) =>
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new ApprovedState().TransitionTo(target, AnyRental())
        );
}
