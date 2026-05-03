using RentalApp.Database.Models;

namespace RentalApp.Database.States;

/// <summary>The owner has accepted the rental request.</summary>
public class ApprovedState : IRentalState
{
    public RentalStatus Status => RentalStatus.Approved;
    public IReadOnlyList<RentalStatus> OwnerTransitions { get; } = [RentalStatus.OutForRent];
    public IReadOnlyList<RentalStatus> BorrowerTransitions { get; } = [];

    public Task<IRentalState> TransitionTo(RentalStatus targetStatus, Rental rental) =>
        targetStatus switch
        {
            RentalStatus.OutForRent => Task.FromResult<IRentalState>(new OutForRentState()),
            _ => throw new InvalidOperationException(
                $"Cannot transition from {Status} to {targetStatus}."
            ),
        };
}
