using RentalApp.Database.Models;

namespace RentalApp.Database.States;

/// <summary>The owner has confirmed the item was returned in good condition. Terminal state.</summary>
public class CompletedState : IRentalState
{
    public RentalStatus Status => RentalStatus.Completed;

    public Task<IRentalState> TransitionTo(RentalStatus targetStatus, Rental rental) =>
        throw new InvalidOperationException(
            $"Cannot transition from {Status}: it is a terminal state."
        );
}
