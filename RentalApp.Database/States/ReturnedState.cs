using RentalApp.Database.Models;

namespace RentalApp.Database.States;

/// <summary>The borrower has marked the item as returned.</summary>
public class ReturnedState : IRentalState
{
    public RentalStatus Status => RentalStatus.Returned;
    public IReadOnlyList<RentalStatus> OwnerTransitions { get; } = [RentalStatus.Completed];
    public IReadOnlyList<RentalStatus> BorrowerTransitions { get; } = [];

    public Task<IRentalState> TransitionTo(RentalStatus targetStatus, Rental rental) =>
        targetStatus switch
        {
            RentalStatus.Completed => Task.FromResult<IRentalState>(new CompletedState()),
            _ => throw new InvalidOperationException(
                $"Cannot transition from {Status} to {targetStatus}."
            ),
        };
}
