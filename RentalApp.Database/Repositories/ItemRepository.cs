using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using RentalApp.Database.Data;
using DbItem = RentalApp.Database.Models.Item;

namespace RentalApp.Database.Repositories;

/// <summary>
/// EF Core / PostGIS implementation of <see cref="IItemRepository"/>.
/// Text search uses <c>EF.Functions.ILike</c> (Npgsql-specific PostgreSQL <c>ILIKE</c>).
/// Proximity queries delegate to PostGIS via NTS <c>IsWithinDistance</c> / <c>Distance</c>.
/// </summary>
public class ItemRepository : IItemRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public ItemRepository(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    private static IQueryable<DbItem> ApplyItemFilters(
        IQueryable<DbItem> query,
        string? category,
        string? search
    )
    {
        if (category != null)
            query = query.Where(i => i.Category.Slug == category);

        if (search != null)
            query = query.Where(i => EF.Functions.ILike(i.Title, $"%{search}%"));

        return query;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<DbItem>> GetItemsAsync(
        string? category,
        string? search,
        int page,
        int pageSize
    )
    {
        await using var context = _contextFactory.CreateDbContext();
        var query = ApplyItemFilters(
            context.Items.Include(i => i.Category).Include(i => i.Owner).AsQueryable(),
            category,
            search
        );

        return await query
            .OrderBy(i => i.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<int> CountItemsAsync(string? category, string? search)
    {
        await using var context = _contextFactory.CreateDbContext();
        var query = ApplyItemFilters(context.Items.AsQueryable(), category, search);
        return await query.CountAsync();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<DbItem>> GetNearbyItemsAsync(
        Point origin,
        double radiusMeters,
        string? category,
        int page,
        int pageSize
    )
    {
        await using var context = _contextFactory.CreateDbContext();
        var query = context
            .Items.Include(i => i.Category)
            .Include(i => i.Owner)
            .Where(i => i.Location.IsWithinDistance(origin, radiusMeters))
            .AsQueryable();

        if (category != null)
            query = query.Where(i => i.Category.Slug == category);

        return await query
            .OrderBy(i => i.Location.Distance(origin))
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<DbItem?> GetItemAsync(int id)
    {
        await using var context = _contextFactory.CreateDbContext();
        return await context
            .Items.Include(i => i.Category)
            .Include(i => i.Owner)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    /// <inheritdoc/>
    /// <remarks>After saving, re-fetches the entity by ID so the returned object includes the populated <c>Category</c> and <c>Owner</c> navigation properties.</remarks>
    public async Task<DbItem> CreateItemAsync(
        string title,
        string? description,
        double dailyRate,
        int categoryId,
        int ownerId,
        Point location
    )
    {
        await using var context = _contextFactory.CreateDbContext();
        var item = new DbItem
        {
            Title = title,
            Description = description,
            DailyRate = dailyRate,
            CategoryId = categoryId,
            OwnerId = ownerId,
            Location = location,
            IsAvailable = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        context.Items.Add(item);
        await context.SaveChangesAsync();

        return await context
                .Items.Include(i => i.Category)
                .Include(i => i.Owner)
                .FirstOrDefaultAsync(i => i.Id == item.Id)
            ?? throw new InvalidOperationException("Failed to retrieve created item.");
    }

    /// <inheritdoc/>
    public async Task<DbItem> UpdateItemAsync(
        int id,
        string? title,
        string? description,
        double? dailyRate,
        bool? isAvailable
    )
    {
        await using var context = _contextFactory.CreateDbContext();
        var item =
            await context
                .Items.Include(i => i.Category)
                .Include(i => i.Owner)
                .FirstOrDefaultAsync(i => i.Id == id)
            ?? throw new InvalidOperationException($"Item {id} not found.");

        if (title != null)
            item.Title = title;
        if (description != null)
            item.Description = description.Length > 0 ? description : null;
        if (dailyRate.HasValue)
            item.DailyRate = dailyRate.Value;
        if (isAvailable.HasValue)
            item.IsAvailable = isAvailable.Value;

        item.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return item;
    }
}
