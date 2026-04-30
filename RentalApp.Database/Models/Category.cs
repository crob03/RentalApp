using System.ComponentModel.DataAnnotations;

namespace RentalApp.Database.Models;

/// <summary>
/// Represents an item category used to organise rental listings.
/// </summary>
public class Category
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>URL-safe identifier used as a filter key in API requests (e.g. "power-tools").</summary>
    [Required]
    public string Slug { get; set; } = string.Empty;

    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
}
