using RentalApp.Database.Models;
using RentalApp.Database.States;

namespace RentalApp.Test.States;

public class RequestedStateTests
{
    private static Rental AnyRental() => new() { Status = "Requested" };

    [Fact]
    public void StateName_IsRequested() =>
        Assert.Equal("Requested", new RequestedState().StateName);

    [Fact]
    public async Task TransitionTo_Approved_ReturnsApprovedState()
    {
        var result = await new RequestedState().TransitionTo("approved", AnyRental());
        Assert.IsType<ApprovedState>(result);
    }

    [Fact]
    public async Task TransitionTo_Rejected_ReturnsRejectedState()
    {
        var result = await new RequestedState().TransitionTo("rejected", AnyRental());
        Assert.IsType<RejectedState>(result);
    }

    [Fact]
    public async Task TransitionTo_IsCaseInsensitive()
    {
        var result = await new RequestedState().TransitionTo("Approved", AnyRental());
        Assert.IsType<ApprovedState>(result);
    }

    [Theory]
    [InlineData("outforrent")]
    [InlineData("returned")]
    [InlineData("completed")]
    [InlineData("overdue")]
    [InlineData("anything")]
    public async Task TransitionTo_InvalidTarget_Throws(string target) =>
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new RequestedState().TransitionTo(target, AnyRental())
        );
}
