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
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public CategoryRepository(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<(DbCategory Category, int ItemCount)>> GetAllAsync()
    {
        await using var context = _contextFactory.CreateDbContext();
        var rows = await context
            .Categories.OrderBy(c => c.Name)
            .GroupJoin(
                context.Items,
                c => c.Id,
                i => i.CategoryId,
                (c, items) => new { Category = c, Count = items.Count() }
            )
            .ToListAsync();

        return rows.Select(r => (r.Category, r.Count));
    }
}
