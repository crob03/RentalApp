using RentalApp.Database.Models;
using RentalApp.Database.States;

namespace RentalApp.Test.States;

public class ReturnedStateTests
{
    private static Rental AnyRental() => new() { Status = "Returned" };

    [Fact]
    public void StateName_IsReturned() => Assert.Equal("Returned", new ReturnedState().StateName);

    [Fact]
    public async Task TransitionTo_Completed_ReturnsCompletedState()
    {
        var result = await new ReturnedState().TransitionTo("completed", AnyRental());
        Assert.IsType<CompletedState>(result);
    }

    [Theory]
    [InlineData("requested")]
    [InlineData("approved")]
    [InlineData("rejected")]
    [InlineData("outforrent")]
    [InlineData("overdue")]
    public async Task TransitionTo_InvalidTarget_Throws(string target) =>
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new ReturnedState().TransitionTo(target, AnyRental())
        );
}
