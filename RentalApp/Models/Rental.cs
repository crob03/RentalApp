namespace RentalApp.Models;

public sealed record Rental(
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
