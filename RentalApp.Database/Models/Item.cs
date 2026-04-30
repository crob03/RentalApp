using System.ComponentModel.DataAnnotations;
using NetTopologySuite.Geometries;

namespace RentalApp.Database.Models;

public class Item
{
    public int Id { get; set; }

    [Required]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    public double DailyRate { get; set; }

    [Required]
    public int CategoryId { get; set; }

    public Category Category { get; set; } = null!;

    [Required]
    public int OwnerId { get; set; }

    public User Owner { get; set; } = null!;

    [Required]
    public Point Location { get; set; } = null!;

    [Required]
    public bool IsAvailable { get; set; } = true;

    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
}
