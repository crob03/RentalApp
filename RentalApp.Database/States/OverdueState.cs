using RentalApp.Database.Models;

namespace RentalApp.Database.States;

/// <summary>The item was not returned by the end date. Set automatically by the repository on read.</summary>
public class OverdueState : IRentalState
{
    public RentalStatus Status => RentalStatus.Overdue;

    public Task<IRentalState> TransitionTo(RentalStatus targetStatus, Rental rental) =>
        targetStatus switch
        {
            RentalStatus.Returned => Task.FromResult<IRentalState>(new ReturnedState()),
            _ => throw new InvalidOperationException(
                $"Cannot transition from {Status} to {targetStatus}."
            ),
        };
}
