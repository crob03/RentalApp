using RentalApp.Database.Models;

namespace RentalApp.Database.States;

/// <summary>Initial state when a borrower submits a rental request.</summary>
public class RequestedState : IRentalState
{
    public RentalStatus Status => RentalStatus.Requested;
    public IReadOnlyList<RentalStatus> OwnerTransitions { get; } =
    [RentalStatus.Approved, RentalStatus.Rejected];
    public IReadOnlyList<RentalStatus> BorrowerTransitions { get; } = [];

    public Task<IRentalState> TransitionTo(RentalStatus targetStatus, Rental rental) =>
        targetStatus switch
        {
            RentalStatus.Approved => Task.FromResult<IRentalState>(new ApprovedState()),
            RentalStatus.Rejected => Task.FromResult<IRentalState>(new RejectedState()),
            _ => throw new InvalidOperationException(
                $"Cannot transition from {Status} to {targetStatus}."
            ),
        };
}
