using RentalApp.Database.Models;
using RentalApp.Database.States;

namespace RentalApp.Test.States;

public class RejectedStateTests
{
    private static Rental AnyRental() => new() { Status = "Rejected" };

    [Fact]
    public void StateName_IsRejected() => Assert.Equal("Rejected", new RejectedState().StateName);

    [Theory]
    [InlineData("approved")]
    [InlineData("outforrent")]
    [InlineData("returned")]
    [InlineData("completed")]
    [InlineData("anything")]
    public async Task TransitionTo_AnyTarget_ThrowsBecauseTerminal(string target) =>
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new RejectedState().TransitionTo(target, AnyRental())
        );
}
