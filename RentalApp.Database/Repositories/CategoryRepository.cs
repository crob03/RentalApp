using Microsoft.EntityFrameworkCore;
using RentalApp.Database.Data;
using DbCategory = RentalApp.Database.Models.Category;

namespace RentalApp.Database.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ICategoryRepository"/>.
/// Uses a GroupJoin (LEFT JOIN semantics) to count items per category in a single query.
/// </summary>
public class CategoryRepository : ICategoryRepository
{
    private readonly AppDbContext _context;

    public CategoryRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
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
