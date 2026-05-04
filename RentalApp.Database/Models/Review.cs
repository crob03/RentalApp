using System.ComponentModel.DataAnnotations;

namespace RentalApp.Database.Models;

public class Review
{
    public int Id { get; set; }

    [Required]
    public int RentalId { get; set; }
    public Rental Rental { get; set; } = null!;

    [Required]
    public int ItemId { get; set; }
    public Item Item { get; set; } = null!;

    [Required]
    public int ReviewerId { get; set; }
    public User Reviewer { get; set; } = null!;

    [Required]
    [Range(1, 5)]
    public int Rating { get; set; }

    [MaxLength(500)]
    public string? Comment { get; set; }

    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
}
