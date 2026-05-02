using Microsoft.EntityFrameworkCore;
using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;
using RentalApp.Database.Data;
using RentalApp.Database.Repositories;
using RentalApp.Http;
using GeoFactory = NetTopologySuite.Geometries.GeometryFactory;
using GeoPoint = NetTopologySuite.Geometries.Point;
using NtsCoordinate = NetTopologySuite.Geometries.Coordinate;
using NtsPrecisionModel = NetTopologySuite.Geometries.PrecisionModel;

namespace RentalApp.Services;

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

    public async Task<NearbyItemsResponse> GetNearbyItemsAsync(GetNearbyItemsRequest request)
    {
        var origin = _geoFactory.CreatePoint(new NtsCoordinate(request.Lon, request.Lat));
        var radiusMeters = request.Radius * 1000;

        var dbItems = await _itemRepository.GetNearbyItemsAsync(origin, radiusMeters, request.Category);
        var items = dbItems.Select(i => ToNearbyItem(i, origin)).ToList();

        return new NearbyItemsResponse(
            items,
            new SearchLocationResponse(request.Lat, request.Lon),
            request.Radius,
            items.Count
        );
    }

    public async Task<ItemDetailResponse> GetItemAsync(int id)
    {
        var dbItem =
            await _itemRepository.GetItemAsync(id)
            ?? throw new InvalidOperationException($"Item {id} not found.");
        return ToItemDetail(dbItem);
    }

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
