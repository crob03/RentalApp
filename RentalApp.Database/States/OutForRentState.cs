using RentalApp.Database.Models;

namespace RentalApp.Database.States;

/// <summary>The item is currently out on rent.</summary>
public class OutForRentState : IRentalState
{
    public RentalStatus Status => RentalStatus.OutForRent;
    public IReadOnlyList<RentalStatus> OwnerTransitions { get; } = [];
    public IReadOnlyList<RentalStatus> BorrowerTransitions { get; } = [RentalStatus.Returned];

    public Task<IRentalState> TransitionTo(RentalStatus targetStatus, Rental rental) =>
        targetStatus switch
        {
            RentalStatus.Overdue => Task.FromResult<IRentalState>(new OverdueState()),
            RentalStatus.Returned => Task.FromResult<IRentalState>(new ReturnedState()),
            _ => throw new InvalidOperationException(
                $"Cannot transition from {Status} to {targetStatus}."
            ),
        };
}
