namespace RentalApp.Contracts.Responses;

/// <summary>Paginated list of rental summaries.</summary>
public record RentalsListResponse(List<RentalSummaryResponse> Rentals, int TotalRentals);

/// <summary>Summary of a single rental, used in list views.</summary>
/// <param name="TotalPrice">Total rental cost in GBP, calculated from the daily rate and rental duration.</param>
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

/// <summary>Full details of a single rental.</summary>
/// <param name="TotalPrice">Total rental cost in GBP, calculated from the daily rate and rental duration.</param>
/// <param name="RequestedAt">Timestamp when the rental request was submitted.</param>
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

/// <summary>Response returned after a rental status update.</summary>
public record UpdateRentalStatusResponse(int Id, string Status, DateTime UpdatedAt);
