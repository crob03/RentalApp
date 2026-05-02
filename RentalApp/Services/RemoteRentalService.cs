using System.Net.Http.Json;
using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;
using RentalApp.Http;

namespace RentalApp.Services;

internal class RemoteRentalService : RemoteServiceBase, IRentalService
{
    private readonly IApiClient _apiClient;

    public RemoteRentalService(IApiClient apiClient) => _apiClient = apiClient;

    public Task<RentalsListResponse> GetIncomingRentalsAsync(GetRentalsRequest request) =>
        GetRentalsAsync("rentals/incoming", request.Status);

    public Task<RentalsListResponse> GetOutgoingRentalsAsync(GetRentalsRequest request) =>
        GetRentalsAsync("rentals/outgoing", request.Status);

    public async Task<RentalDetailResponse> GetRentalAsync(int id)
    {
        var response = await _apiClient.GetAsync($"rentals/{id}");
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<RentalDetailResponse>()
            ?? throw new InvalidOperationException("Empty rental response from API");
    }

    public async Task<RentalSummaryResponse> CreateRentalAsync(CreateRentalRequest request)
    {
        var response = await _apiClient.PostAsJsonAsync(
            "rentals",
            new
            {
                itemId = request.ItemId,
                startDate = request.StartDate.ToString(
                    "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture
                ),
                endDate = request.EndDate.ToString(
                    "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture
                ),
            }
        );
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<RentalSummaryResponse>()
            ?? throw new InvalidOperationException("Empty create rental response from API");
    }

    public async Task<UpdateRentalStatusResponse> UpdateRentalStatusAsync(
        int id,
        UpdateRentalStatusRequest request
    )
    {
        var response = await _apiClient.PatchAsJsonAsync(
            $"rentals/{id}/status",
            new { status = request.Status }
        );
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<UpdateRentalStatusResponse>()
            ?? throw new InvalidOperationException("Empty update status response from API");
    }

    private async Task<RentalsListResponse> GetRentalsAsync(string path, string? status)
    {
        var query = status != null ? $"{path}?status={Uri.EscapeDataString(status)}" : path;

        var response = await _apiClient.GetAsync(query);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<RentalsListResponse>()
            ?? throw new InvalidOperationException("Empty rentals response from API");
    }
}
