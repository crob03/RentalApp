using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;

namespace RentalApp.Services;

public interface IItemService
{
    Task<ItemsResponse> GetItemsAsync(GetItemsRequest request);
    Task<NearbyItemsResponse> GetNearbyItemsAsync(GetNearbyItemsRequest request);
    Task<ItemDetailResponse> GetItemAsync(int id);
    Task<CreateItemResponse> CreateItemAsync(CreateItemRequest request);
    Task<UpdateItemResponse> UpdateItemAsync(int id, UpdateItemRequest request);
    Task<CategoriesResponse> GetCategoriesAsync();
}
