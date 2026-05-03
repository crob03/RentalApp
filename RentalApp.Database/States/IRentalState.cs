using RentalApp.Database.Models;

namespace RentalApp.Database.States;

/// <summary>
/// Represents a state in the rental lifecycle state machine.
/// </summary>
public interface IRentalState
{
    /// <summary>The status value for this state.</summary>
    RentalStatus Status { get; }

    /// <summary>Status values the item owner may transition this rental to.</summary>
    IReadOnlyList<RentalStatus> OwnerTransitions { get; }

    /// <summary>Status values the borrower may transition this rental to.</summary>
    IReadOnlyList<RentalStatus> BorrowerTransitions { get; }

    /// <summary>
    /// Attempts to transition to the given target status.
    /// </summary>
    /// <param name="targetStatus">Desired target status.</param>
    /// <param name="rental">The rental being transitioned.</param>
    /// <returns>The new state after a valid transition.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the transition is not valid from this state.</exception>
    Task<IRentalState> TransitionTo(RentalStatus targetStatus, Rental rental);
}
