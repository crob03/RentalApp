using RentalApp.Database.Models;

namespace RentalApp.Database.States;

/// <summary>Initial state when a borrower submits a rental request.</summary>
public class RequestedState : IRentalState
{
    public string StateName => "Requested";

    public Task<IRentalState> TransitionTo(string targetStatus, Rental rental) =>
        targetStatus.ToLower() switch
        {
            "approved" => Task.FromResult<IRentalState>(new ApprovedState()),
            "rejected" => Task.FromResult<IRentalState>(new RejectedState()),
            _ => throw new InvalidOperationException(
                $"Cannot transition from {StateName} to {targetStatus}."
            ),
        };
}
