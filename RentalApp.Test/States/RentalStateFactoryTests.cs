using RentalApp.Database.Models;
using RentalApp.Database.States;

namespace RentalApp.Test.States;

public class RentalStateFactoryTests
{
    [Theory]
    [InlineData(RentalStatus.Requested, typeof(RequestedState))]
    [InlineData(RentalStatus.Approved, typeof(ApprovedState))]
    [InlineData(RentalStatus.Rejected, typeof(RejectedState))]
    [InlineData(RentalStatus.OutForRent, typeof(OutForRentState))]
    [InlineData(RentalStatus.Overdue, typeof(OverdueState))]
    [InlineData(RentalStatus.Returned, typeof(ReturnedState))]
    [InlineData(RentalStatus.Completed, typeof(CompletedState))]
    public void From_KnownStatus_ReturnsCorrectType(RentalStatus status, Type expectedType)
    {
        var state = RentalStateFactory.From(status);
        Assert.IsType(expectedType, state);
    }

    [Fact]
    public void From_OutOfRangeStatus_Throws() =>
        Assert.Throws<InvalidOperationException>(() => RentalStateFactory.From((RentalStatus)999));
}
