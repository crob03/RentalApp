using RentalApp.Contracts.Requests;
using RentalApp.Contracts.Responses;
using RentalApp.Database.Repositories;
using RentalApp.Database.States;
using RentalApp.Services.Auth;
using DbRental = RentalApp.Database.Models.Rental;

namespace RentalApp.Services.Rentals;

/// <summary>
/// Repository-backed implementation of <see cref="IRentalService"/> for local/offline development.
/// </summary>
internal class LocalRentalService : IRentalService
{
    private readonly IRentalRepository _rentalRepository;
    private readonly IItemRepository _itemRepository;
    private readonly AuthTokenState _tokenState;

    public LocalRentalService(
        IRentalRepository rentalRepository,
        IItemRepository itemRepository,
        AuthTokenState tokenState
    )
    {
        _rentalRepository = rentalRepository;
        _itemRepository = itemRepository;
        _tokenState = tokenState;
    }

    /// <inheritdoc/>
    public async Task<RentalsListResponse> GetIncomingRentalsAsync(GetRentalsRequest request)
    {
        var currentUserId = GetCurrentUserId();
        var rentals = (await _rentalRepository.GetIncomingRentalsAsync(currentUserId)).ToList();
        await PromoteOverdueRentalsAsync(rentals);
        var filtered =
            request.Status == null
                ? rentals
                : rentals.Where(r => r.Status == ParseStatus(request.Status)).ToList();
        var summaries = filtered.Select(ToRentalSummary).ToList();
        return new RentalsListResponse(summaries, summaries.Count);
    }

    /// <inheritdoc/>
    public async Task<RentalsListResponse> GetOutgoingRentalsAsync(GetRentalsRequest request)
    {
        var currentUserId = GetCurrentUserId();
        var rentals = (await _rentalRepository.GetOutgoingRentalsAsync(currentUserId)).ToList();
        await PromoteOverdueRentalsAsync(rentals);
        var filtered =
            request.Status == null
                ? rentals
                : rentals.Where(r => r.Status == ParseStatus(request.Status)).ToList();
        var summaries = filtered.Select(ToRentalSummary).ToList();
        return new RentalsListResponse(summaries, summaries.Count);
    }

    /// <inheritdoc/>
    public async Task<RentalDetailResponse> GetRentalAsync(int id)
    {
        var currentUserId = GetCurrentUserId();
        var rental =
            await _rentalRepository.GetRentalAsync(id)
            ?? throw new InvalidOperationException($"Rental {id} not found.");

        if (rental.OwnerId != currentUserId && rental.BorrowerId != currentUserId)
            throw new UnauthorizedAccessException("You do not have access to this rental.");

        await PromoteOverdueRentalsAsync([rental]);
        return ToRentalDetail(rental);
    }

    /// <inheritdoc/>
    public async Task<RentalSummaryResponse> CreateRentalAsync(CreateRentalRequest request)
    {
        var currentUserId = GetCurrentUserId();

        var item =
            await _itemRepository.GetItemAsync(request.ItemId)
            ?? throw new InvalidOperationException($"Item {request.ItemId} not found.");

        if (item.OwnerId == currentUserId)
            throw new InvalidOperationException("You cannot rent your own item.");

        if (request.StartDate >= request.EndDate)
            throw new InvalidOperationException("End date must be after start date.");

        if (request.StartDate < DateOnly.FromDateTime(DateTime.UtcNow.Date))
            throw new InvalidOperationException("Start date cannot be in the past.");

        if (
            await _rentalRepository.HasOverlappingRentalAsync(
                request.ItemId,
                request.StartDate,
                request.EndDate
            )
        )
            throw new InvalidOperationException(
                "The item is already booked for the requested dates."
            );

        var rental = await _rentalRepository.CreateRentalAsync(
            request.ItemId,
            item.OwnerId,
            currentUserId,
            request.StartDate,
            request.EndDate
        );

        return ToRentalSummary(rental);
    }

    /// <inheritdoc/>
    public async Task<UpdateRentalStatusResponse> UpdateRentalStatusAsync(
        int id,
        UpdateRentalStatusRequest request
    )
    {
        var currentUserId = GetCurrentUserId();
        var rental =
            await _rentalRepository.GetRentalAsync(id)
            ?? throw new InvalidOperationException($"Rental {id} not found.");

        if (rental.OwnerId != currentUserId && rental.BorrowerId != currentUserId)
            throw new UnauthorizedAccessException("You do not have access to this rental.");

        var targetStatus = ParseStatus(request.Status);

        if (targetStatus == RentalStatus.Overdue)
            throw new InvalidOperationException("The overdue status is set automatically.");

        var isOwner = rental.OwnerId == currentUserId;
        RentalStatus[] ownerOnlyStatuses =
        [
            RentalStatus.Approved,
            RentalStatus.Rejected,
            RentalStatus.OutForRent,
            RentalStatus.Completed,
        ];

        if (ownerOnlyStatuses.Contains(targetStatus) && !isOwner)
            throw new UnauthorizedAccessException("Only the owner can perform this transition.");

        if (targetStatus == RentalStatus.Returned && isOwner)
            throw new UnauthorizedAccessException(
                "Only the borrower can mark an item as returned."
            );

        var newState = await RentalStateFactory
            .From(rental.Status)
            .TransitionTo(targetStatus, rental);

        var updated = await _rentalRepository.UpdateRentalStatusAsync(id, newState.Status);

        return new UpdateRentalStatusResponse(
            updated.Id,
            updated.Status.ToString(),
            updated.UpdatedAt ?? DateTime.UtcNow
        );
    }

    private async Task PromoteOverdueRentalsAsync(IList<DbRental> rentals)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        foreach (
            var rental in rentals.Where(r =>
                r.Status == RentalStatus.OutForRent && r.EndDate < today
            )
        )
        {
            await _rentalRepository.UpdateRentalStatusAsync(rental.Id, RentalStatus.Overdue);
            rental.Status = RentalStatus.Overdue;
        }
    }

    private int GetCurrentUserId() =>
        int.Parse(
            _tokenState.CurrentToken
                ?? throw new InvalidOperationException("No user is authenticated.")
        );

    private static RentalStatus ParseStatus(string status)
    {
        if (!Enum.TryParse<RentalStatus>(status, ignoreCase: true, out var result))
            throw new InvalidOperationException($"Unknown rental status: '{status}'.");
        return result;
    }

    private static RentalSummaryResponse ToRentalSummary(DbRental r) =>
        new(
            r.Id,
            r.ItemId,
            r.Item.Title,
            r.BorrowerId,
            $"{r.Borrower.FirstName} {r.Borrower.LastName}",
            r.OwnerId,
            $"{r.Owner.FirstName} {r.Owner.LastName}",
            r.StartDate,
            r.EndDate,
            r.Status.ToString(),
            TotalPrice: r.Item.DailyRate * (r.EndDate.DayNumber - r.StartDate.DayNumber),
            r.CreatedAt ?? DateTime.UtcNow
        );

    private static RentalDetailResponse ToRentalDetail(DbRental r) =>
        new(
            r.Id,
            r.ItemId,
            r.Item.Title,
            r.Item.Description,
            r.BorrowerId,
            $"{r.Borrower.FirstName} {r.Borrower.LastName}",
            r.OwnerId,
            $"{r.Owner.FirstName} {r.Owner.LastName}",
            r.StartDate,
            r.EndDate,
            r.Status.ToString(),
            TotalPrice: r.Item.DailyRate * (r.EndDate.DayNumber - r.StartDate.DayNumber),
            RequestedAt: r.CreatedAt ?? DateTime.UtcNow
        );
}
