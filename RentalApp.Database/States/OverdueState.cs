using RentalApp.Database.Models;

namespace RentalApp.Database.States;

/// <summary>The item was not returned by the end date. Set automatically by the repository on read.</summary>
public class OverdueState : IRentalState
{
    public string StateName => "Overdue";

    public Task<IRentalState> TransitionTo(string targetStatus, Rental rental) =>
        targetStatus.ToLower() switch
        {
            "returned" => Task.FromResult<IRentalState>(new ReturnedState()),
            _ => throw new InvalidOperationException(
                $"Cannot transition from {StateName} to {targetStatus}."
            ),
        };
}
