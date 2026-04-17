namespace RentalApp.Models;

/// <summary>
/// Represents an item category returned by <c>GET /categories</c>.
/// </summary>
/// <param name="Id">Unique category identifier.</param>
/// <param name="Name">Display name of the category.</param>
/// <param name="Slug">URL-safe identifier used as a query parameter when filtering items.</param>
/// <param name="ItemCount">Number of items currently listed in this category.</param>
public sealed record Category(int Id, string Name, string Slug, int ItemCount);
