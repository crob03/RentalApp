using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;

namespace RentalApp.Services;

/// <summary>
/// Defines the contract for creating and managing item rentals.
/// </summary>
public interface IRentalService
{
    /// <summary>
    /// Returns the rentals where the authenticated user is the item owner (items being rented out).
    /// </summary>
    /// <param name="request">Optional status filter and pagination parameters.</param>
    /// <returns>A list of incoming rental requests or active rentals for the owner's items.</returns>
    Task<RentalsListResponse> GetIncomingRentalsAsync(GetRentalsRequest request);

    /// <summary>
    /// Returns the rentals where the authenticated user is the renter (items being borrowed).
    /// </summary>
    /// <param name="request">Optional status filter and pagination parameters.</param>
    /// <returns>A list of rental requests or active rentals initiated by the current user.</returns>
    Task<RentalsListResponse> GetOutgoingRentalsAsync(GetRentalsRequest request);

    /// <summary>
    /// Returns the full detail of a single rental by its identifier.
    /// </summary>
    /// <param name="id">The rental's unique identifier.</param>
    /// <returns>Full rental detail including item, renter, owner, dates, and current status.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no rental with the given <paramref name="id"/> exists.</exception>
    Task<RentalDetailResponse> GetRentalAsync(int id);

    /// <summary>
    /// Creates a new rental request for an item.
    /// </summary>
    /// <param name="request">The item identifier together with the requested start and end dates.</param>
    /// <returns>A summary of the created rental including its assigned identifier and initial status.</returns>
    Task<RentalSummaryResponse> CreateRentalAsync(CreateRentalRequest request);

    /// <summary>
    /// Transitions a rental to a new status (e.g. accepted, rejected, or completed).
    /// </summary>
    /// <param name="id">The unique identifier of the rental to update.</param>
    /// <param name="request">The target status to apply.</param>
    /// <returns>The rental reflecting the updated status.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no rental with the given <paramref name="id"/> exists.</exception>
    Task<UpdateRentalStatusResponse> UpdateRentalStatusAsync(
        int id,
        UpdateRentalStatusRequest request
    );
}
