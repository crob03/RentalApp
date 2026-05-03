using RentalApp.Database.Models;
using RentalApp.Database.States;

namespace RentalApp.Test.States;

public class RejectedStateTests
{
    private static Rental AnyRental() => new() { Status = RentalStatus.Rejected };

    [Fact]
    public void Status_IsRejected() =>
        Assert.Equal(RentalStatus.Rejected, new RejectedState().Status);

    [Theory]
    [InlineData(RentalStatus.Approved)]
    [InlineData(RentalStatus.OutForRent)]
    [InlineData(RentalStatus.Returned)]
    [InlineData(RentalStatus.Completed)]
    public async Task TransitionTo_AnyTarget_ThrowsBecauseTerminal(RentalStatus target) =>
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new RejectedState().TransitionTo(target, AnyRental())
        );
}
