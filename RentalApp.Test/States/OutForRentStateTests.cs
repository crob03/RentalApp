using RentalApp.Database.Models;
using RentalApp.Database.States;

namespace RentalApp.Test.States;

public class OutForRentStateTests
{
    private static Rental AnyRental() => new() { Status = "OutForRent" };

    [Fact]
    public void StateName_IsOutForRent() =>
        Assert.Equal("OutForRent", new OutForRentState().StateName);

    [Fact]
    public async Task TransitionTo_Overdue_ReturnsOverdueState()
    {
        var result = await new OutForRentState().TransitionTo("overdue", AnyRental());
        Assert.IsType<OverdueState>(result);
    }

    [Fact]
    public async Task TransitionTo_Returned_ReturnsReturnedState()
    {
        var result = await new OutForRentState().TransitionTo("returned", AnyRental());
        Assert.IsType<ReturnedState>(result);
    }

    [Theory]
    [InlineData("requested")]
    [InlineData("approved")]
    [InlineData("rejected")]
    [InlineData("completed")]
    [InlineData("anything")]
    public async Task TransitionTo_InvalidTarget_Throws(string target) =>
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new OutForRentState().TransitionTo(target, AnyRental())
        );
}
