namespace RentalApp.Models;

/// <summary>
/// Represents a rental request between a borrower and an item owner.
/// </summary>
/// <param name="Id">Unique rental identifier.</param>
/// <param name="ItemId">Identifier of the item being rented.</param>
/// <param name="ItemTitle">Title of the item being rented.</param>
/// <param name="ItemDescription">Description of the item; only present in detail responses (<c>GET /rentals/{id}</c>).</param>
/// <param name="BorrowerId">User ID of the borrower.</param>
/// <param name="BorrowerName">Display name of the borrower.</param>
/// <param name="OwnerId">User ID of the item owner.</param>
/// <param name="OwnerName">Display name of the item owner.</param>
/// <param name="StartDate">First day of the rental period.</param>
/// <param name="EndDate">Last day of the rental period.</param>
/// <param name="Status">Current rental status (e.g. <c>pending</c>, <c>approved</c>, <c>completed</c>).</param>
/// <param name="TotalPrice">Total price calculated from the daily rate and rental duration.</param>
/// <param name="RequestedAt">Timestamp when the rental request was created.</param>
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
