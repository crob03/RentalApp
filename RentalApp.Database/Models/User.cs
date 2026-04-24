using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RentalApp.Database.Models;

/// <summary>
/// Represents a registered user of the application.
/// Maps to the <c>users</c> table in the database.
/// </summary>
[Table("users")]
[PrimaryKey(nameof(Id))]
public class User
{
    /// <summary>
    /// Gets or sets the unique identifier for the user.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the user's first name. Maximum 100 characters.
    /// </summary>
    [Required]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's last name. Maximum 100 characters.
    /// </summary>
    [Required]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's email address. Must be unique across all users. Maximum 255 characters.
    /// </summary>
    [Required]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the BCrypt hash of the user's password.
    /// </summary>
    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the BCrypt salt used when hashing the user's password.
    /// </summary>
    [Required]
    public string PasswordSalt { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC timestamp when the user account was created.
    /// </summary>
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the UTC timestamp when the user account was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
}
