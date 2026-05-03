using RentalApp.Database.Models;

namespace RentalApp.Database.States;

/// <summary>The owner has accepted the rental request.</summary>
public class ApprovedState : IRentalState
{
    public string StateName => "Approved";

    public Task<IRentalState> TransitionTo(string targetStatus, Rental rental) =>
        targetStatus.ToLower() switch
        {
            "outforrent" => Task.FromResult<IRentalState>(new OutForRentState()),
            _ => throw new InvalidOperationException(
                $"Cannot transition from {StateName} to {targetStatus}."
            ),
        };
}
