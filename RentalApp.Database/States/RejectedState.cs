using RentalApp.Database.Models;

namespace RentalApp.Database.States;

/// <summary>The owner declined the request. Terminal state.</summary>
public class RejectedState : IRentalState
{
    public string StateName => "Rejected";

    public Task<IRentalState> TransitionTo(string targetStatus, Rental rental) =>
        throw new InvalidOperationException(
            $"Cannot transition from {StateName}: it is a terminal state."
        );
}
