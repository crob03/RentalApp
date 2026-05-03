using RentalApp.Database.Models;

namespace RentalApp.Database.States;

/// <summary>The borrower has marked the item as returned.</summary>
public class ReturnedState : IRentalState
{
    public string StateName => "Returned";

    public Task<IRentalState> TransitionTo(string targetStatus, Rental rental) =>
        targetStatus.ToLower() switch
        {
            "completed" => Task.FromResult<IRentalState>(new CompletedState()),
            _ => throw new InvalidOperationException(
                $"Cannot transition from {StateName} to {targetStatus}."
            ),
        };
}
