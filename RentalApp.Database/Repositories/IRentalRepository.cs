using RentalApp.Database.Models;
using RentalApp.Database.States;

namespace RentalApp.Database.Repositories;

/// <summary>
/// Data-access contract for rental queries and mutations.
/// </summary>
public interface IRentalRepository
{
    /// <summary>
    /// Returns the rental with the given <paramref name="id"/>, including its <c>Item</c>, <c>Owner</c>,
    /// and <c>Borrower</c> navigation properties, or <see langword="null"/> if not found.
    /// </summary>
    Task<Rental?> GetRentalAsync(int id);

    /// <summary>Returns all rentals where the given user is the owner, ordered by creation date descending.</summary>
    Task<IEnumerable<Rental>> GetIncomingRentalsAsync(int ownerId);

    /// <summary>Returns all rentals where the given user is the borrower, ordered by creation date descending.</summary>
    Task<IEnumerable<Rental>> GetOutgoingRentalsAsync(int borrowerId);

    /// <summary>
    /// Inserts a new rental with status <c>Requested</c> and returns the fully-hydrated entity.
    /// </summary>
    Task<Rental> CreateRentalAsync(
        int itemId,
        int ownerId,
        int borrowerId,
        DateOnly startDate,
        DateOnly endDate
    );

    /// <summary>
    /// Sets the <c>Status</c> and <c>UpdatedAt</c> of the rental with the given <paramref name="id"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if no rental with <paramref name="id"/> exists.</exception>
    Task<Rental> UpdateRentalStatusAsync(int id, RentalStatus status);

    /// <summary>
    /// Returns <see langword="true"/> if any active rental for <paramref name="itemId"/> overlaps the given date range.
    /// Active statuses are: <c>Requested</c>, <c>Approved</c>, <c>OutForRent</c>, <c>Overdue</c>.
    /// Overlap condition: <c>startDate &lt; existingEndDate &amp;&amp; endDate &gt; existingStartDate</c>
    /// (same-day turnaround is allowed).
    /// </summary>
    Task<bool> HasOverlappingRentalAsync(int itemId, DateOnly startDate, DateOnly endDate);
}
