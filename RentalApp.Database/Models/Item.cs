using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RentalApp.Database.Models;

[Table("items")]
[PrimaryKey(nameof(Id))]
public class Item
{
    public int Id { get; set; }

    [Required]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    public double DailyRate { get; set; } = 0.0;

    [Required]
    public int CategoryId { get; set; } = 0;

    public Category Category { get; set; } = null!;

    [Required]
    public int OwnerId { get; set; } = 0;

    public User Owner { get; set; } = null!;

    [Required]
    public double Latitude { get; set; } = 0.0;

    [Required]
    public double Longitude { get; set; } = 0.0;

    [Required]
    public string ImageUrl { get; set; } = string.Empty;
}
