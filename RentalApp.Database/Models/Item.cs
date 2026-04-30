using System.ComponentModel.DataAnnotations;
using NetTopologySuite.Geometries;

namespace RentalApp.Database.Models;

/// <summary>
/// Represents a rental item listing created by a user.
/// </summary>
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

    /// <summary>Geographic coordinates of the item stored as a PostGIS <c>geometry(Point, 4326)</c> column. Used for proximity queries via <c>IsWithinDistance</c>.</summary>
    [Required]
    public Point Location { get; set; } = null!;

    [Required]
    public bool IsAvailable { get; set; } = true;

    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
}
