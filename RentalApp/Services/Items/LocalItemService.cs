using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;
using RentalApp.Database.Repositories;
using RentalApp.Services.Auth;
using DbItem = RentalApp.Database.Models.Item;
using GeoFactory = NetTopologySuite.Geometries.GeometryFactory;
using NtsCoordinate = NetTopologySuite.Geometries.Coordinate;
using NtsPrecisionModel = NetTopologySuite.Geometries.PrecisionModel;

namespace RentalApp.Services.Items;

/// <summary>
/// Repository-backed implementation of <see cref="IItemService"/> for local/offline development.
/// Reads and writes directly to the database via <see cref="IItemRepository"/> and <see cref="ICategoryRepository"/>.
/// </summary>
internal class LocalItemService : IItemService
{
    private readonly IItemRepository _itemRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly AuthTokenState _tokenState;

    private static readonly GeoFactory _geoFactory = new(new NtsPrecisionModel(), 4326);

    public LocalItemService(
        IItemRepository itemRepository,
        ICategoryRepository categoryRepository,
        AuthTokenState tokenState
    )
    {
        _itemRepository = itemRepository;
        _categoryRepository = categoryRepository;
        _tokenState = tokenState;
    }

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
        var radiusMeters = request.Radius * 1000;

        var dbItems = await _itemRepository.GetNearbyItemsAsync(
            origin,
            radiusMeters,
            request.Category
        );
        var items = dbItems.Select(r => ToNearbyItem(r.Item, r.DistanceMeters)).ToList();

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
        var categories = await _categoryRepository.GetAllAsync();
        var countsByCategoryId = await _itemRepository.CountItemsByCategoryAsync();
        var response = categories
            .Select(c => new CategoryResponse(
                c.Id,
                c.Name,
                c.Slug,
                countsByCategoryId.GetValueOrDefault(c.Id, 0)
            ))
            .ToList();
        return new CategoriesResponse(response);
    }

    private static ItemSummaryResponse ToItemSummary(DbItem i) =>
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

    private static NearbyItemResponse ToNearbyItem(DbItem i, double distanceMeters) =>
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
            Distance: distanceMeters / 1000.0,
            i.IsAvailable,
            AverageRating: null
        );

    private static ItemDetailResponse ToItemDetail(DbItem i) =>
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
