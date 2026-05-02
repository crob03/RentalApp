using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;

namespace RentalApp.Services;

internal class LocalRentalService : IRentalService
{
    public Task<RentalsListResponse> GetIncomingRentalsAsync(GetRentalsRequest request) =>
        throw new NotImplementedException("Rental support requires local DB entities");

    public Task<RentalsListResponse> GetOutgoingRentalsAsync(GetRentalsRequest request) =>
        throw new NotImplementedException("Rental support requires local DB entities");

    public Task<RentalDetailResponse> GetRentalAsync(int id) =>
        throw new NotImplementedException("Rental support requires local DB entities");

    public Task<RentalSummaryResponse> CreateRentalAsync(CreateRentalRequest request) =>
        throw new NotImplementedException("Rental support requires local DB entities");

    public Task<UpdateRentalStatusResponse> UpdateRentalStatusAsync(
        int id,
        UpdateRentalStatusRequest request
    ) => throw new NotImplementedException("Rental support requires local DB entities");
}
