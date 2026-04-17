namespace RentalApp.Models;

/// <summary>
/// Represents a review submitted by a borrower for a completed rental.
/// </summary>
/// <remarks>
/// A single type covers all review contexts. Fields not returned by a particular
/// endpoint are <see langword="null"/> — for example, <see cref="RentalId"/> is absent
/// when reviews are embedded in item or user profile responses.
/// </remarks>
/// <param name="Id">Unique review identifier.</param>
/// <param name="RentalId">The rental this review was submitted for; <see langword="null"/> when embedded in item or profile responses.</param>
/// <param name="ItemId">The item being reviewed; <see langword="null"/> when not returned by the endpoint.</param>
/// <param name="ReviewerId">User ID of the reviewer; <see langword="null"/> when embedded in public user profile responses.</param>
/// <param name="Rating">Numeric rating from 1 to 5.</param>
/// <param name="ItemTitle">Title of the reviewed item; <see langword="null"/> when not returned by the endpoint.</param>
/// <param name="Comment">Optional written comment.</param>
/// <param name="ReviewerName">Display name of the reviewer.</param>
/// <param name="CreatedAt">Timestamp when the review was submitted.</param>
public sealed record Review(
    int Id,
    int? RentalId,
    int? ItemId,
    int? ReviewerId,
    int Rating,
    string? ItemTitle,
    string? Comment,
    string ReviewerName,
    DateTime CreatedAt
);
