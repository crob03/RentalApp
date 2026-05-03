using System.ComponentModel.DataAnnotations;

namespace RentalApp.Database.Models;

/// <summary>
/// Represents a rental agreement between an item owner and a borrower.
/// </summary>
public class Rental
{
    public int Id { get; set; }

    [Required]
    public int ItemId { get; set; }

    public Item Item { get; set; } = null!;

    [Required]
    public int OwnerId { get; set; }

    public User Owner { get; set; } = null!;

    [Required]
    public int BorrowerId { get; set; }

    public User Borrower { get; set; } = null!;

    [Required]
    public DateOnly StartDate { get; set; }

    [Required]
    public DateOnly EndDate { get; set; }

    [Required]
    public string Status { get; set; } = "Requested";

    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
}
