namespace RentalApp.Contracts.Requests;

/// <summary>
/// Request parameters for retrieving a list of rentals, with an optional status filter and pagination.
/// </summary>
/// <param name="Status">When provided, only rentals with this status are returned.</param>
/// <param name="Page">Page number (1-indexed).</param>
/// <param name="PageSize">Number of items per page.</param>
public record GetRentalsRequest(string? Status = null, int Page = 1, int PageSize = 20);

/// <summary>Request payload for creating a new rental.</summary>
public record CreateRentalRequest(int ItemId, DateOnly StartDate, DateOnly EndDate);

/// <summary>Request payload for updating the status of an existing rental.</summary>
public record UpdateRentalStatusRequest(string Status);
