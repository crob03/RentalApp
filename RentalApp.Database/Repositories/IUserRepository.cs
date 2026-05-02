using DbUser = RentalApp.Database.Models.User;

namespace RentalApp.Database.Repositories;

/// <summary>
/// Data-access contract for user queries and mutations.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Returns the user with the given <paramref name="email"/>,
    /// or <see langword="null"/> if not found.
    /// </summary>
    Task<DbUser?> GetByEmailAsync(string email);

    /// <summary>
    /// Returns the user with the given <paramref name="id"/>,
    /// or <see langword="null"/> if not found.
    /// </summary>
    Task<DbUser?> GetByIdAsync(int id);

    /// <summary>
    /// Persists a new user and returns the saved entity with its assigned ID.
    /// </summary>
    Task<DbUser> CreateAsync(
        string firstName,
        string lastName,
        string email,
        string passwordHash,
        string passwordSalt
    );
}
