using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;

namespace RentalApp.Services.Rentals;

/// <summary>
/// Stub implementation of <see cref="IRentalService"/> for local/offline mode.
/// All methods throw <see cref="NotImplementedException"/> — Rental DB entities are not yet implemented.
/// </summary>
internal class LocalRentalService : IRentalService
{
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
}
