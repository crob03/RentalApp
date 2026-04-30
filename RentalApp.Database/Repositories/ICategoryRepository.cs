using DbCategory = RentalApp.Database.Models.Category;

namespace RentalApp.Database.Repositories;

/// <summary>
/// Data-access contract for category queries.
/// </summary>
public interface ICategoryRepository
{
    /// <summary>
    /// Returns all categories ordered by name, each paired with the total number of items in that category.
    /// </summary>
    Task<IEnumerable<(DbCategory Category, int ItemCount)>> GetAllAsync();
}
