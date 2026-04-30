using Microsoft.EntityFrameworkCore;
using RentalApp.Database.Data;
using DbCategory = RentalApp.Database.Models.Category;

namespace RentalApp.Database.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly AppDbContext _context;

    public CategoryRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<(DbCategory Category, int ItemCount)>> GetAllAsync()
    {
        var rows = await _context
            .Categories.OrderBy(c => c.Name)
            .GroupJoin(
                _context.Items,
                c => c.Id,
                i => i.CategoryId,
                (c, items) => new { Category = c, Count = items.Count() }
            )
            .ToListAsync();

        return rows.Select(r => (r.Category, r.Count));
    }
}
