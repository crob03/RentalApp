using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using RentalApp.Database.Data;
using RentalApp.Database.Repositories;
using RentalApp.Models;
using DbUser = RentalApp.Database.Models.User;
using GeoFactory = NetTopologySuite.Geometries.GeometryFactory;
using GeoPoint = NetTopologySuite.Geometries.Point;
using NtsCoordinate = NetTopologySuite.Geometries.Coordinate;
using NtsPrecisionModel = NetTopologySuite.Geometries.PrecisionModel;

namespace RentalApp.Services;

public class LocalApiService : IApiService
{
    private readonly AppDbContext _context;
    private readonly IItemRepository _itemRepository;
    private readonly ICategoryRepository _categoryRepository;
    private User? _currentUser;

    private static readonly GeoFactory _geoFactory = new GeoFactory(new NtsPrecisionModel(), 4326);

    public LocalApiService(
        AppDbContext context,
        IItemRepository itemRepository,
        ICategoryRepository categoryRepository
    )
    {
        _context = context;
        _itemRepository = itemRepository;
        _categoryRepository = categoryRepository;
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

    public async Task<List<Item>> GetItemsAsync(
        string? category = null,
        string? search = null,
        int page = 1,
        int pageSize = 20
    )
    {
        var dbItems = await _itemRepository.GetItemsAsync(category, search, page, pageSize);
        return dbItems.Select(ToItem).ToList();
    }

    public async Task<List<Item>> GetNearbyItemsAsync(
        double lat,
        double lon,
        double radius = 5.0,
        string? category = null,
        int page = 1,
        int pageSize = 20
    )
    {
        var origin = _geoFactory.CreatePoint(new NtsCoordinate(lon, lat));
        var radiusMeters = radius * 1000;

        var dbItems = await _itemRepository.GetNearbyItemsAsync(
            origin,
            radiusMeters,
            category,
            page,
            pageSize
        );

        return dbItems.Select(i => ToNearbyItem(i, origin)).ToList();
    }

    public async Task<Item> GetItemAsync(int id)
    {
        var dbItem =
            await _itemRepository.GetItemAsync(id)
            ?? throw new InvalidOperationException($"Item {id} not found.");

        return ToItem(dbItem);
    }

    public async Task<Item> CreateItemAsync(
        string title,
        string? description,
        double dailyRate,
        int categoryId,
        double latitude,
        double longitude
    )
    {
        if (_currentUser == null)
            throw new InvalidOperationException("No user is currently authenticated");

        var location = _geoFactory.CreatePoint(new NtsCoordinate(longitude, latitude));
        var dbItem = await _itemRepository.CreateItemAsync(
            title,
            description,
            dailyRate,
            categoryId,
            _currentUser.Id,
            location
        );

        return ToItem(dbItem);
    }

    public async Task<Item> UpdateItemAsync(
        int id,
        string? title,
        string? description,
        double? dailyRate,
        bool? isAvailable
    )
    {
        var dbItem = await _itemRepository.UpdateItemAsync(
            id,
            title,
            description,
            dailyRate,
            isAvailable
        );

        return ToItem(dbItem);
    }

    public async Task<List<Category>> GetCategoriesAsync()
    {
        var results = await _categoryRepository.GetAllAsync();
        return results
            .Select(r => new Category(
                r.Category.Id,
                r.Category.Name,
                r.Category.Slug,
                ItemCount: r.ItemCount
            ))
            .ToList();
    }

    private static User ToUser(DbUser user) =>
        new(user.Id, user.FirstName, user.LastName, 0.0, 0, 0, user.Email, user.CreatedAt, null);

    private static Item ToItem(Database.Models.Item i) =>
        new(
            i.Id,
            i.Title,
            i.Description,
            i.DailyRate,
            i.CategoryId,
            i.Category.Name,
            i.OwnerId,
            $"{i.Owner.FirstName} {i.Owner.LastName}",
            OwnerRating: null,
            Latitude: i.Location.Y,
            Longitude: i.Location.X,
            Distance: null,
            i.IsAvailable,
            AverageRating: null,
            TotalReviews: null,
            i.CreatedAt,
            Reviews: null
        );

    private static Item ToNearbyItem(Database.Models.Item i, GeoPoint origin) =>
        new(
            i.Id,
            i.Title,
            i.Description,
            i.DailyRate,
            i.CategoryId,
            i.Category.Name,
            i.OwnerId,
            $"{i.Owner.FirstName} {i.Owner.LastName}",
            OwnerRating: null,
            Latitude: i.Location.Y,
            Longitude: i.Location.X,
            Distance: i.Location.Distance(origin) / 1000.0,
            i.IsAvailable,
            AverageRating: null,
            TotalReviews: null,
            CreatedAt: null,
            Reviews: null
        );
}
