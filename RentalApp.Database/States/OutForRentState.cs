using RentalApp.Database.Models;

namespace RentalApp.Database.States;

/// <summary>The item is currently out on rent.</summary>
public class OutForRentState : IRentalState
{
    public string StateName => "OutForRent";

    public Task<IRentalState> TransitionTo(string targetStatus, Rental rental) =>
        targetStatus.ToLower() switch
        {
            "overdue" => Task.FromResult<IRentalState>(new OverdueState()),
            "returned" => Task.FromResult<IRentalState>(new ReturnedState()),
            _ => throw new InvalidOperationException(
                $"Cannot transition from {StateName} to {targetStatus}."
            ),
        };
}
