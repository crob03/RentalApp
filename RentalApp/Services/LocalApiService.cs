using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using RentalApp.Database.Data;
using RentalApp.Models;
using DbUser = RentalApp.Database.Models.User;

namespace RentalApp.Services;

/// <summary>
/// <see cref="IApiService"/> implementation backed by a local PostgreSQL database via EF Core.
/// </summary>
public class LocalApiService : IApiService
{
    private readonly AppDbContext _context;
    private User? _currentUser;

    /// <summary>Initialises a new instance of <see cref="LocalApiService"/>.</summary>
    /// <param name="context">EF Core database context used to query and persist user data.</param>
    public LocalApiService(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    /// <remarks>Verifies the password against the stored BCrypt hash. The authenticated user is cached in memory for the duration of the session.</remarks>
    public async Task LoginAsync(string email, string password)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password");

        _currentUser = ToUser(user);
    }

    /// <inheritdoc/>
    /// <remarks>Generates a BCrypt salt and hashes the password before persisting to the database.</remarks>
    public async Task RegisterAsync(
        string firstName,
        string lastName,
        string email,
        string password
    )
    {
        var existing = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (existing != null)
            throw new InvalidOperationException("User with this email already exists");

        var salt = BCrypt.Net.BCrypt.GenerateSalt();
        _context.Users.Add(
            new DbUser
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, salt),
                PasswordSalt = salt,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }
        );
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc/>
    /// <remarks>Returns the user cached by the most recent <see cref="LoginAsync"/> call. Throws if no session is active.</remarks>
    public Task<User> GetCurrentUserAsync()
    {
        if (_currentUser == null)
            throw new InvalidOperationException("No user is currently authenticated");

        return Task.FromResult(_currentUser);
    }

    /// <inheritdoc/>
    public async Task<User> GetUserAsync(int userId)
    {
        var user =
            await _context.Users.FindAsync(userId)
            ?? throw new InvalidOperationException($"User {userId} not found");

        return ToUser(user);
    }

    /// <inheritdoc/>
    /// <remarks>Clears the in-memory user cache. No database operation is performed.</remarks>
    public Task LogoutAsync()
    {
        _currentUser = null;
        return Task.CompletedTask;
    }

    private static User ToUser(DbUser user) =>
        new(user.Id, user.FirstName, user.LastName, 0.0, 0, 0, user.Email, user.CreatedAt, null);

    public Task<List<Item>> GetItemsAsync(
        string? category = null,
        string? search = null,
        int page = 1
    ) => throw new NotImplementedException();

    public Task<List<Item>> GetNearbyItemsAsync(
        double lat,
        double lon,
        double radius = 5.0,
        string? category = null
    ) => throw new NotImplementedException();

    public Task<Item> GetItemAsync(int id) => throw new NotImplementedException();

    public Task<Item> CreateItemAsync(
        string title,
        string? description,
        double dailyRate,
        int categoryId,
        double latitude,
        double longitude
    ) => throw new NotImplementedException();

    public Task<Item> UpdateItemAsync(
        int id,
        string? title,
        string? description,
        double? dailyRate,
        bool? isAvailable
    ) => throw new NotImplementedException();

    public Task<List<Category>> GetCategoriesAsync() => throw new NotImplementedException();
}
