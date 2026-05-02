using Microsoft.EntityFrameworkCore;
using RentalApp.Database.Data;
using DbCategory = RentalApp.Database.Models.Category;

namespace RentalApp.Database.Repositories;

/// <summary>EF Core implementation of <see cref="ICategoryRepository"/>.</summary>
public class CategoryRepository : ICategoryRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public CategoryRepository(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<DbCategory>> GetAllAsync()
    {
        await using var context = _contextFactory.CreateDbContext();
        return await context.Categories.OrderBy(c => c.Name).ToListAsync();
    }
}
