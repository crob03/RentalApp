using RentalApp.Database.States;

namespace RentalApp.Test.States;

public class RentalStateFactoryTests
{
    [Theory]
    [InlineData("Requested", typeof(RequestedState))]
    [InlineData("Approved", typeof(ApprovedState))]
    [InlineData("Rejected", typeof(RejectedState))]
    [InlineData("OutForRent", typeof(OutForRentState))]
    [InlineData("Overdue", typeof(OverdueState))]
    [InlineData("Returned", typeof(ReturnedState))]
    [InlineData("Completed", typeof(CompletedState))]
    public void FromString_KnownStatus_ReturnsCorrectType(string status, Type expectedType)
    {
        var state = RentalStateFactory.FromString(status);
        Assert.IsType(expectedType, state);
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("")]
    [InlineData("requested")]
    public void FromString_UnknownStatus_Throws(string status) =>
        Assert.Throws<InvalidOperationException>(() => RentalStateFactory.FromString(status));
}
