namespace RentalApp.Contracts.Requests;

public record GetRentalsRequest(string? Status = null);

public record CreateRentalRequest(int ItemId, DateOnly StartDate, DateOnly EndDate);

public record UpdateRentalStatusRequest(string Status);
