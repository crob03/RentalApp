using DbCategory = RentalApp.Database.Models.Category;

namespace RentalApp.Database.Repositories;

public interface ICategoryRepository
{
    Task<IEnumerable<(DbCategory Category, int ItemCount)>> GetAllAsync();
}
