namespace RentalApp.Contracts.Responses;

/// <summary>Paginated list of rental summaries.</summary>
public record RentalsListResponse(List<RentalSummaryResponse> Rentals, int TotalRentals);

/// <summary>
/// Summary of a single rental, used in list views. Incoming rentals (owner's view) populate
/// borrower fields and leave owner fields null; outgoing rentals (borrower's view) do the reverse.
/// </summary>
/// <param name="TotalPrice">Total rental cost in GBP, calculated from the daily rate and rental duration.</param>
/// <param name="RequestedAt">Timestamp when the rental request was submitted.</param>
public record RentalSummaryResponse(
    int Id,
    int ItemId,
    string ItemTitle,
    int? BorrowerId,
    string? BorrowerName,
    double? BorrowerRating,
    int? OwnerId,
    string? OwnerName,
    double? OwnerRating,
    DateTime StartDate,
    DateTime EndDate,
    string Status,
    double TotalPrice,
    DateTime RequestedAt
);

/// <summary>Response returned after creating a new rental. Contains both borrower and owner fields.</summary>
/// <param name="TotalPrice">Total rental cost in GBP, calculated from the daily rate and rental duration.</param>
/// <param name="CreatedAt">Timestamp when the rental was created.</param>
public record CreateRentalResponse(
    int Id,
    int ItemId,
    string ItemTitle,
    int BorrowerId,
    string BorrowerName,
    int OwnerId,
    string OwnerName,
    DateTime StartDate,
    DateTime EndDate,
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
