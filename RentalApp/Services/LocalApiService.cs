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

/// <summary>
/// <see cref="IApiService"/> implementation that operates directly against the local PostgreSQL
/// database via EF Core repositories. Used for offline development and integration testing in
/// place of the remote HTTP API.
/// </summary>
public class LocalApiService : IApiService
{
    private readonly AppDbContext _context;
    private readonly IItemRepository _itemRepository;
    private readonly ICategoryRepository _categoryRepository;
    private User? _currentUser;

    /// <summary>Shared NTS geometry factory configured for SRID 4326 (WGS 84). Reused across all point creation calls to avoid repeated allocation.</summary>
    private static readonly GeoFactory _geoFactory = new GeoFactory(new NtsPrecisionModel(), 4326);

    /// <summary>
    /// Initialises a new instance of <see cref="LocalApiService"/> with the required data-access dependencies.
    /// </summary>
    /// <param name="context">EF Core database context, used directly for user operations not covered by repositories.</param>
    /// <param name="itemRepository">Repository for item queries and mutations.</param>
    /// <param name="categoryRepository">Repository for category queries.</param>
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

    /// <inheritdoc/>
    /// <remarks>Verifies the BCrypt password hash and stores the authenticated user in <c>_currentUser</c> for the lifetime of this service instance.</remarks>
    public async Task LoginAsync(string email, string password)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password");

        _currentUser = ToUser(user);
    }

    /// <inheritdoc/>
    /// <remarks>Generates a fresh BCrypt salt per registration so no two users share a hash even with the same password.</remarks>
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
    /// <remarks>Returns the in-memory <c>_currentUser</c> set during <see cref="LoginAsync"/>; no database call is made.</remarks>
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
    /// <remarks>Clears the in-memory <c>_currentUser</c>. No database call is made.</remarks>
    public Task LogoutAsync()
    {
        _currentUser = null;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    /// <remarks>
    /// Converts <paramref name="radius"/> from kilometres to metres before passing it to the repository,
    /// as PostGIS geography distance functions operate in metres.
    /// The NTS <c>Point</c> is constructed as <c>(X=longitude, Y=latitude)</c> — NTS coordinate order is (lon, lat).
    /// </remarks>
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

    /// <inheritdoc/>
    public async Task<Item> GetItemAsync(int id)
    {
        var dbItem =
            await _itemRepository.GetItemAsync(id)
            ?? throw new InvalidOperationException($"Item {id} not found.");

        return ToItem(dbItem);
    }

    /// <inheritdoc/>
    /// <remarks>Uses <c>_currentUser.Id</c> as the owner ID; throws if no user is logged in.</remarks>
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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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

    /// <summary>Maps a database <see cref="DbUser"/> to the application <see cref="User"/> model. Rating and rental stats are zeroed as they are not stored on the user entity.</summary>
    private static User ToUser(DbUser user) =>
        new(user.Id, user.FirstName, user.LastName, 0.0, 0, 0, user.Email, user.CreatedAt, null);

    /// <summary>
    /// Maps a database item to the application <see cref="Item"/> model.
    /// NTS stores coordinates as <c>(X=longitude, Y=latitude)</c>, so <c>Location.Y</c> is latitude and <c>Location.X</c> is longitude.
    /// </summary>
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

    /// <summary>
    /// Maps a database item to the application <see cref="Item"/> model with the distance from <paramref name="origin"/> populated.
    /// Distance is converted from metres (returned by NTS) to kilometres by dividing by 1000.
    /// </summary>
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
