// RentalApp/Services/LocalApiService.cs
using Microsoft.EntityFrameworkCore;
using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;
using RentalApp.Database.Data;
using RentalApp.Database.Repositories;
using RentalApp.Http;
using DbUser = RentalApp.Database.Models.User;
using GeoFactory = NetTopologySuite.Geometries.GeometryFactory;
using GeoPoint = NetTopologySuite.Geometries.Point;
using NtsCoordinate = NetTopologySuite.Geometries.Coordinate;
using NtsPrecisionModel = NetTopologySuite.Geometries.PrecisionModel;

namespace RentalApp.Services;

/// <summary>
/// Implements <see cref="IApiService"/> against a local PostgreSQL database for offline development.
/// Rental and review methods throw <see cref="NotImplementedException"/> until the corresponding DB entities are added.
/// </summary>
public class LocalApiService : IApiService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly IItemRepository _itemRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly AuthTokenState _tokenState;

    private static readonly GeoFactory _geoFactory = new GeoFactory(new NtsPrecisionModel(), 4326);

    public LocalApiService(
        IDbContextFactory<AppDbContext> contextFactory,
        IItemRepository itemRepository,
        ICategoryRepository categoryRepository,
        AuthTokenState tokenState
    )
    {
        _contextFactory = contextFactory;
        _itemRepository = itemRepository;
        _categoryRepository = categoryRepository;
        _tokenState = tokenState;
    }

    // ── Auth ──────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        await using var context = _contextFactory.CreateDbContext();
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password");

        // AuthenticationService writes this to AuthTokenState after login
        return new LoginResponse(
            Token: user.Id.ToString(),
            ExpiresAt: DateTime.MaxValue,
            UserId: user.Id
        );
    }

    /// <inheritdoc/>
    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
    {
        await using var context = _contextFactory.CreateDbContext();
        var existing = await context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (existing != null)
            throw new InvalidOperationException("User with this email already exists");

        var salt = BCrypt.Net.BCrypt.GenerateSalt();
        var newUser = new DbUser
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, salt),
            PasswordSalt = salt,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        context.Users.Add(newUser);
        await context.SaveChangesAsync();

        return new RegisterResponse(
            newUser.Id,
            newUser.Email,
            newUser.FirstName,
            newUser.LastName,
            newUser.CreatedAt ?? DateTime.UtcNow
        );
    }

    /// <inheritdoc/>
    public async Task<CurrentUserResponse> GetCurrentUserAsync()
    {
        if (!_tokenState.HasSession)
            throw new InvalidOperationException("No user is currently authenticated");

        var userId = int.Parse(_tokenState.CurrentToken!);
        await using var context = _contextFactory.CreateDbContext();
        var user =
            await context.Users.FindAsync(userId)
            ?? throw new InvalidOperationException("Authenticated user not found");

        var itemsListed = await context.Items.CountAsync(i => i.OwnerId == userId);

        return new CurrentUserResponse(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            AverageRating: null,
            ItemsListed: itemsListed,
            RentalsCompleted: 0,
            user.CreatedAt ?? DateTime.UtcNow
        );
    }

    /// <inheritdoc/>
    public async Task<UserProfileResponse> GetUserProfileAsync(int userId)
    {
        await using var context = _contextFactory.CreateDbContext();
        var user =
            await context.Users.FindAsync(userId)
            ?? throw new InvalidOperationException($"User {userId} not found");

        var itemsListed = await context.Items.CountAsync(i => i.OwnerId == userId);

        return new UserProfileResponse(
            user.Id,
            user.FirstName,
            user.LastName,
            AverageRating: null,
            ItemsListed: itemsListed,
            RentalsCompleted: 0,
            Reviews: []
        );
    }

    // ── Items ─────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<ItemsResponse> GetItemsAsync(GetItemsRequest request)
    {
        var totalItems = await _itemRepository.CountItemsAsync(request.Category, request.Search);
        var dbItems = await _itemRepository.GetItemsAsync(
            request.Category,
            request.Search,
            request.Page,
            request.PageSize
        );
        var items = dbItems.Select(ToItemSummary).ToList();
        var totalPages =
            request.PageSize > 0 ? (int)Math.Ceiling((double)totalItems / request.PageSize) : 0;
        return new ItemsResponse(
            items,
            TotalItems: totalItems,
            Page: request.Page,
            PageSize: request.PageSize,
            TotalPages: totalPages
        );
    }

    /// <inheritdoc/>
    public async Task<NearbyItemsResponse> GetNearbyItemsAsync(GetNearbyItemsRequest request)
    {
        var origin = _geoFactory.CreatePoint(new NtsCoordinate(request.Lon, request.Lat));
        var radiusMeters = request.Radius * 1000; // km → metres

        var dbItems = await _itemRepository.GetNearbyItemsAsync(
            origin,
            radiusMeters,
            request.Category,
            1,
            int.MaxValue
        );
        var items = dbItems.Select(i => ToNearbyItem(i, origin)).ToList();

        return new NearbyItemsResponse(
            items,
            new SearchLocationResponse(request.Lat, request.Lon),
            request.Radius,
            items.Count
        );
    }

    /// <inheritdoc/>
    public async Task<ItemDetailResponse> GetItemAsync(int id)
    {
        var dbItem =
            await _itemRepository.GetItemAsync(id)
            ?? throw new InvalidOperationException($"Item {id} not found.");
        return ToItemDetail(dbItem);
    }

    /// <inheritdoc/>
    public async Task<CreateItemResponse> CreateItemAsync(CreateItemRequest request)
    {
        if (!_tokenState.HasSession)
            throw new InvalidOperationException("No user is currently authenticated");

        var ownerId = int.Parse(_tokenState.CurrentToken!);
        var location = _geoFactory.CreatePoint(
            new NtsCoordinate(request.Longitude, request.Latitude)
        );
        var dbItem = await _itemRepository.CreateItemAsync(
            request.Title,
            request.Description,
            request.DailyRate,
            request.CategoryId,
            ownerId,
            location
        );

        return new CreateItemResponse(
            dbItem.Id,
            dbItem.Title,
            dbItem.Description,
            dbItem.DailyRate,
            dbItem.CategoryId,
            dbItem.Category.Name,
            dbItem.OwnerId,
            $"{dbItem.Owner.FirstName} {dbItem.Owner.LastName}",
            request.Latitude,
            request.Longitude,
            dbItem.IsAvailable,
            dbItem.CreatedAt ?? DateTime.UtcNow
        );
    }

    /// <inheritdoc/>
    public async Task<UpdateItemResponse> UpdateItemAsync(int id, UpdateItemRequest request)
    {
        var dbItem = await _itemRepository.UpdateItemAsync(
            id,
            request.Title,
            request.Description,
            request.DailyRate,
            request.IsAvailable
        );
        return new UpdateItemResponse(
            dbItem.Id,
            dbItem.Title,
            dbItem.Description ?? string.Empty,
            dbItem.DailyRate,
            dbItem.IsAvailable
        );
    }

    /// <inheritdoc/>
    public async Task<CategoriesResponse> GetCategoriesAsync()
    {
        var results = await _categoryRepository.GetAllAsync();
        var categories = results
            .Select(r => new CategoryResponse(
                r.Category.Id,
                r.Category.Name,
                r.Category.Slug,
                r.ItemCount
            ))
            .ToList();
        return new CategoriesResponse(categories);
    }

    // ── Rentals / Reviews — not yet supported locally ─────────────────

    /// <inheritdoc/>
    public Task<RentalsListResponse> GetIncomingRentalsAsync(GetRentalsRequest request) =>
        throw new NotImplementedException("Rental support requires local DB entities");

    /// <inheritdoc/>
    public Task<RentalsListResponse> GetOutgoingRentalsAsync(GetRentalsRequest request) =>
        throw new NotImplementedException("Rental support requires local DB entities");

    /// <inheritdoc/>
    public Task<RentalDetailResponse> GetRentalAsync(int id) =>
        throw new NotImplementedException("Rental support requires local DB entities");

    /// <inheritdoc/>
    public Task<RentalSummaryResponse> CreateRentalAsync(CreateRentalRequest request) =>
        throw new NotImplementedException("Rental support requires local DB entities");

    /// <inheritdoc/>
    public Task<UpdateRentalStatusResponse> UpdateRentalStatusAsync(
        int id,
        UpdateRentalStatusRequest request
    ) => throw new NotImplementedException("Rental support requires local DB entities");

    /// <inheritdoc/>
    public Task<ReviewsResponse> GetItemReviewsAsync(int itemId, GetReviewsRequest request) =>
        throw new NotImplementedException("Review support requires local DB entities");

    /// <inheritdoc/>
    public Task<ReviewsResponse> GetUserReviewsAsync(int userId, GetReviewsRequest request) =>
        throw new NotImplementedException("Review support requires local DB entities");

    /// <inheritdoc/>
    public Task<CreateReviewResponse> CreateReviewAsync(CreateReviewRequest request) =>
        throw new NotImplementedException("Review support requires local DB entities");

    // ── Mapping helpers ───────────────────────────────────────────────

    private static ItemSummaryResponse ToItemSummary(Database.Models.Item i) =>
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
            i.IsAvailable,
            AverageRating: null,
            i.CreatedAt ?? DateTime.UtcNow
        );

    private static NearbyItemResponse ToNearbyItem(Database.Models.Item i, GeoPoint origin) =>
        new(
            i.Id,
            i.Title,
            i.Description,
            i.DailyRate,
            i.CategoryId,
            i.Category.Name,
            i.OwnerId,
            $"{i.Owner.FirstName} {i.Owner.LastName}",
            Latitude: i.Location.Y,
            Longitude: i.Location.X,
            Distance: i.Location.Distance(origin) / 1000.0,
            i.IsAvailable,
            AverageRating: null
        );

    private static ItemDetailResponse ToItemDetail(Database.Models.Item i) =>
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
            i.IsAvailable,
            AverageRating: null,
            TotalReviews: 0,
            i.CreatedAt ?? DateTime.UtcNow,
            Reviews: []
        );
}
