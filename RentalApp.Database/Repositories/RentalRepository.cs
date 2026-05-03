using Microsoft.EntityFrameworkCore;
using RentalApp.Database.Data;
using RentalApp.Database.Models;
using RentalApp.Database.States;

namespace RentalApp.Database.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IRentalRepository"/>.
/// </summary>
public class RentalRepository : IRentalRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public RentalRepository(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    /// <inheritdoc/>
    public async Task<Rental?> GetRentalAsync(int id)
    {
        await using var context = _contextFactory.CreateDbContext();
        return await context
            .Rentals.Include(r => r.Item)
            .Include(r => r.Owner)
            .Include(r => r.Borrower)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Rental>> GetIncomingRentalsAsync(int ownerId)
    {
        await using var context = _contextFactory.CreateDbContext();
        return await context
            .Rentals.Include(r => r.Item)
            .Include(r => r.Owner)
            .Include(r => r.Borrower)
            .Where(r => r.OwnerId == ownerId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Rental>> GetOutgoingRentalsAsync(int borrowerId)
    {
        await using var context = _contextFactory.CreateDbContext();
        return await context
            .Rentals.Include(r => r.Item)
            .Include(r => r.Owner)
            .Include(r => r.Borrower)
            .Where(r => r.BorrowerId == borrowerId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<Rental> CreateRentalAsync(
        int itemId,
        int ownerId,
        int borrowerId,
        DateOnly startDate,
        DateOnly endDate
    )
    {
        await using var context = _contextFactory.CreateDbContext();
        var rental = new Rental
        {
            ItemId = itemId,
            OwnerId = ownerId,
            BorrowerId = borrowerId,
            StartDate = startDate,
            EndDate = endDate,
            Status = RentalStatus.Requested,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        context.Rentals.Add(rental);
        await context.SaveChangesAsync();

        return await context
                .Rentals.Include(r => r.Item)
                .Include(r => r.Owner)
                .Include(r => r.Borrower)
                .FirstOrDefaultAsync(r => r.Id == rental.Id)
            ?? throw new InvalidOperationException("Failed to retrieve created rental.");
    }

    /// <inheritdoc/>
    public async Task<Rental> UpdateRentalStatusAsync(int id, RentalStatus status)
    {
        await using var context = _contextFactory.CreateDbContext();
        var rental =
            await context
                .Rentals.Include(r => r.Item)
                .Include(r => r.Owner)
                .Include(r => r.Borrower)
                .FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new InvalidOperationException($"Rental {id} not found.");

        rental.Status = status;
        rental.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return rental;
    }

    /// <inheritdoc/>
    public async Task<bool> HasOverlappingRentalAsync(
        int itemId,
        DateOnly startDate,
        DateOnly endDate
    )
    {
        await using var context = _contextFactory.CreateDbContext();
        var activeStatuses = new[]
        {
            RentalStatus.Requested,
            RentalStatus.Approved,
            RentalStatus.OutForRent,
            RentalStatus.Overdue,
        };
        return await context.Rentals.AnyAsync(r =>
            r.ItemId == itemId
            && activeStatuses.Contains(r.Status)
            && startDate < r.EndDate
            && endDate > r.StartDate
        );
    }
}
