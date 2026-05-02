using DbCategory = RentalApp.Database.Models.Category;

namespace RentalApp.Database.Repositories;

/// <summary>
/// Data-access contract for category queries.
/// </summary>
public interface ICategoryRepository
{
    /// <summary>Returns all categories ordered by name.</summary>
    Task<IEnumerable<DbCategory>> GetAllAsync();
}
