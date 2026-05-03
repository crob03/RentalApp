using RentalApp.Database.Models;
using RentalApp.Database.States;

namespace RentalApp.Test.States;

public class CompletedStateTests
{
    private static Rental AnyRental() => new() { Status = "Completed" };

    [Fact]
    public void StateName_IsCompleted() =>
        Assert.Equal("Completed", new CompletedState().StateName);

    [Theory]
    [InlineData("requested")]
    [InlineData("approved")]
    [InlineData("returned")]
    [InlineData("anything")]
    public async Task TransitionTo_AnyTarget_ThrowsBecauseTerminal(string target) =>
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new CompletedState().TransitionTo(target, AnyRental())
        );
}
