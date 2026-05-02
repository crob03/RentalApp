namespace RentalApp.Contracts.Requests;

/// <summary>
/// Request parameters for retrieving a list of rentals, with an optional status filter.
/// </summary>
/// <param name="Status">When provided, only rentals with this status are returned.</param>
public record GetRentalsRequest(string? Status = null);

/// <summary>Request payload for creating a new rental.</summary>
public record CreateRentalRequest(int ItemId, DateOnly StartDate, DateOnly EndDate);

/// <summary>Request payload for updating the status of an existing rental.</summary>
public record UpdateRentalStatusRequest(string Status);
