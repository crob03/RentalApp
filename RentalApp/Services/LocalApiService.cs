using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using RentalApp.Database.Data;
using RentalApp.Models;
using DbUser = RentalApp.Database.Models.User;

namespace RentalApp.Services;

public class LocalApiService : IApiService
{
    private readonly AppDbContext _context;
    private User? _currentUser;

    public LocalApiService(AppDbContext context)
    {
        _context = context;
    }

    public async Task LoginAsync(string email, string password)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password");

        _currentUser = ToUser(user);
    }

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

    public Task<User> GetCurrentUserAsync()
    {
        if (_currentUser == null)
            throw new InvalidOperationException("No user is currently authenticated");

        return Task.FromResult(_currentUser);
    }

    public async Task<User> GetUserAsync(int userId)
    {
        var user =
            await _context.Users.FindAsync(userId)
            ?? throw new InvalidOperationException($"User {userId} not found");

        return ToUser(user);
    }

    public Task LogoutAsync()
    {
        _currentUser = null;
        return Task.CompletedTask;
    }

    private static User ToUser(DbUser user) =>
        new(user.Id, user.FirstName, user.LastName, 0.0, 0, 0, user.Email, user.CreatedAt, null);

    // ── Future domain methods ──────────────────────────────────────
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

    public Task<Rental> RequestRentalAsync(int itemId, DateOnly startDate, DateOnly endDate) =>
        throw new NotImplementedException();

    public Task<List<Rental>> GetIncomingRentalsAsync(string? status = null) =>
        throw new NotImplementedException();

    public Task<List<Rental>> GetOutgoingRentalsAsync(string? status = null) =>
        throw new NotImplementedException();

    public Task<Rental> GetRentalAsync(int id) => throw new NotImplementedException();

    public Task UpdateRentalStatusAsync(int rentalId, string status) =>
        throw new NotImplementedException();

    public Task<Review> CreateReviewAsync(int rentalId, int rating, string comment) =>
        throw new NotImplementedException();

    public Task<List<Review>> GetItemReviewsAsync(int itemId, int page = 1) =>
        throw new NotImplementedException();

    public Task<List<Review>> GetUserReviewsAsync(int userId, int page = 1) =>
        throw new NotImplementedException();
}
