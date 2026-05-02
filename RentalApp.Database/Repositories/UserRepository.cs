using Microsoft.EntityFrameworkCore;
using RentalApp.Database.Data;
using DbUser = RentalApp.Database.Models.User;

namespace RentalApp.Database.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IUserRepository"/>.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public UserRepository(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    /// <inheritdoc/>
    public async Task<DbUser?> GetByEmailAsync(string email)
    {
        var normalized = email.ToLowerInvariant();
        await using var context = _contextFactory.CreateDbContext();
        return await context.Users.FirstOrDefaultAsync(u => u.Email == normalized);
    }

    /// <inheritdoc/>
    public async Task<DbUser?> GetByIdAsync(int id)
    {
        await using var context = _contextFactory.CreateDbContext();
        return await context.Users.FindAsync(id);
    }

    /// <inheritdoc/>
    public async Task<DbUser> CreateAsync(
        string firstName,
        string lastName,
        string email,
        string passwordHash,
        string passwordSalt
    )
    {
        await using var context = _contextFactory.CreateDbContext();
        var user = new DbUser
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email.ToLowerInvariant(),
            PasswordHash = passwordHash,
            PasswordSalt = passwordSalt,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }
}
