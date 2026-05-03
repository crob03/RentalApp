using RentalApp.Database.Models;

namespace RentalApp.Database.States;

/// <summary>
/// Maps a <see cref="RentalStatus"/> to the corresponding <see cref="IRentalState"/> instance.
/// </summary>
public static class RentalStateFactory
{
    /// <summary>
    /// Returns the state object for the given <paramref name="status"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown for unknown status values.</exception>
    public static IRentalState From(RentalStatus status) =>
        status switch
        {
            RentalStatus.Requested => new RequestedState(),
            RentalStatus.Approved => new ApprovedState(),
            RentalStatus.Rejected => new RejectedState(),
            RentalStatus.OutForRent => new OutForRentState(),
            RentalStatus.Overdue => new OverdueState(),
            RentalStatus.Returned => new ReturnedState(),
            RentalStatus.Completed => new CompletedState(),
            _ => throw new InvalidOperationException($"Unknown rental status: {status}."),
        };
}
