namespace RentalApp.Contracts.Responses;

public record CategoriesResponse(List<CategoryResponse> Categories);

public record CategoryResponse(int Id, string Name, string Slug, int ItemCount);
