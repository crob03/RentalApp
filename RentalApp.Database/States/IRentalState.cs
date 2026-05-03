using RentalApp.Database.Models;

namespace RentalApp.Database.States;

/// <summary>
/// Represents a state in the rental lifecycle state machine.
/// </summary>
public interface IRentalState
{
    /// <summary>The canonical status string stored in the database.</summary>
    string StateName { get; }

    /// <summary>
    /// Attempts to transition to the given target status.
    /// </summary>
    /// <param name="targetStatus">Desired target status (case-insensitive).</param>
    /// <param name="rental">The rental being transitioned.</param>
    /// <returns>The new state after a valid transition.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the transition is not valid from this state.</exception>
    Task<IRentalState> TransitionTo(string targetStatus, Rental rental);
}
