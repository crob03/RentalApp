using RentalApp.Database.Models;
using RentalApp.Database.States;

namespace RentalApp.Test.States;

public class ApprovedStateTests
{
    private static Rental AnyRental() => new() { Status = "Approved" };

    [Fact]
    public void StateName_IsApproved() => Assert.Equal("Approved", new ApprovedState().StateName);

    [Fact]
    public async Task TransitionTo_OutForRent_ReturnsOutForRentState()
    {
        var result = await new ApprovedState().TransitionTo("OutForRent", AnyRental());
        Assert.IsType<OutForRentState>(result);
    }

    [Fact]
    public async Task TransitionTo_IsCaseInsensitive()
    {
        var result = await new ApprovedState().TransitionTo("OUTFORRENT", AnyRental());
        Assert.IsType<OutForRentState>(result);
    }

    [Theory]
    [InlineData("requested")]
    [InlineData("rejected")]
    [InlineData("returned")]
    [InlineData("completed")]
    [InlineData("overdue")]
    public async Task TransitionTo_InvalidTarget_Throws(string target) =>
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new ApprovedState().TransitionTo(target, AnyRental())
        );
}
