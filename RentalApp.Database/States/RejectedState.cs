using RentalApp.Database.Models;

namespace RentalApp.Database.States;

/// <summary>The owner declined the request. Terminal state.</summary>
public class RejectedState : IRentalState
{
    public RentalStatus Status => RentalStatus.Rejected;
    public IReadOnlyList<RentalStatus> OwnerTransitions { get; } = [];
    public IReadOnlyList<RentalStatus> BorrowerTransitions { get; } = [];

    public Task<IRentalState> TransitionTo(RentalStatus targetStatus, Rental rental) =>
        throw new InvalidOperationException(
            $"Cannot transition from {Status}: it is a terminal state."
        );
}
