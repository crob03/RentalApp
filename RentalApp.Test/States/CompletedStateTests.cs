using RentalApp.Database.Models;
using RentalApp.Database.States;

namespace RentalApp.Test.States;

public class CompletedStateTests
{
    private static Rental AnyRental() => new() { Status = RentalStatus.Completed };

    [Fact]
    public void Status_IsCompleted() =>
        Assert.Equal(RentalStatus.Completed, new CompletedState().Status);

    [Theory]
    [InlineData(RentalStatus.Requested)]
    [InlineData(RentalStatus.Approved)]
    [InlineData(RentalStatus.Returned)]
    public async Task TransitionTo_AnyTarget_ThrowsBecauseTerminal(RentalStatus target) =>
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new CompletedState().TransitionTo(target, AnyRental())
        );

    [Fact]
    public void OwnerTransitions_IsEmpty() => Assert.Empty(new CompletedState().OwnerTransitions);

    [Fact]
    public void BorrowerTransitions_IsEmpty() =>
        Assert.Empty(new CompletedState().BorrowerTransitions);
}
