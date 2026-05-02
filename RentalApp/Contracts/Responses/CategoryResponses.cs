namespace RentalApp.Contracts.Responses;

/// <summary>Response containing all available item categories.</summary>
public record CategoriesResponse(List<CategoryResponse> Categories);

/// <summary>Summary of a single item category.</summary>
/// <param name="ItemCount">Number of items currently listed under this category.</param>
public record CategoryResponse(int Id, string Name, string Slug, int ItemCount);
