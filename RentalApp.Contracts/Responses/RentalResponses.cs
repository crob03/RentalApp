namespace RentalApp.Contracts.Responses;

public record RentalsListResponse(List<RentalSummaryResponse> Rentals, int TotalRentals);

public record RentalSummaryResponse(
    int Id,
    int ItemId,
    string ItemTitle,
    int BorrowerId,
    string BorrowerName,
    int OwnerId,
    string OwnerName,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status,
    double TotalPrice,
    DateTime CreatedAt
);

public record RentalDetailResponse(
    int Id,
    int ItemId,
    string ItemTitle,
    string? ItemDescription,
    int BorrowerId,
    string BorrowerName,
    int OwnerId,
    string OwnerName,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status,
    double TotalPrice,
    DateTime RequestedAt
);

public record UpdateRentalStatusResponse(int Id, string Status, DateTime UpdatedAt);
