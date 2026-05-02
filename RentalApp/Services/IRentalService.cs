using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;

namespace RentalApp.Services;

public interface IRentalService
{
    Task<RentalsListResponse> GetIncomingRentalsAsync(GetRentalsRequest request);
    Task<RentalsListResponse> GetOutgoingRentalsAsync(GetRentalsRequest request);
    Task<RentalDetailResponse> GetRentalAsync(int id);
    Task<RentalSummaryResponse> CreateRentalAsync(CreateRentalRequest request);
    Task<UpdateRentalStatusResponse> UpdateRentalStatusAsync(
        int id,
        UpdateRentalStatusRequest request
    );
}
