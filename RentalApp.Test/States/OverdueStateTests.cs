using RentalApp.Database.Models;
using RentalApp.Database.States;

namespace RentalApp.Test.States;

public class OverdueStateTests
{
    private static Rental AnyRental() => new() { Status = "Overdue" };

    [Fact]
    public void StateName_IsOverdue() => Assert.Equal("Overdue", new OverdueState().StateName);

    [Fact]
    public async Task TransitionTo_Returned_ReturnsReturnedState()
    {
        var result = await new OverdueState().TransitionTo("returned", AnyRental());
        Assert.IsType<ReturnedState>(result);
    }

    [Theory]
    [InlineData("requested")]
    [InlineData("approved")]
    [InlineData("rejected")]
    [InlineData("outforrent")]
    [InlineData("completed")]
    public async Task TransitionTo_InvalidTarget_Throws(string target) =>
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new OverdueState().TransitionTo(target, AnyRental())
        );
}
