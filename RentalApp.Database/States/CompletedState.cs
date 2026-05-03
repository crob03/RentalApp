using RentalApp.Database.Models;

namespace RentalApp.Database.States;

/// <summary>The owner has confirmed the item was returned in good condition. Terminal state.</summary>
public class CompletedState : IRentalState
{
    public string StateName => "Completed";

    public Task<IRentalState> TransitionTo(string targetStatus, Rental rental) =>
        throw new InvalidOperationException(
            $"Cannot transition from {StateName}: it is a terminal state."
        );
}
