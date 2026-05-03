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
    public Task<RentalsListResponse> GetIncomingRentalsAsync(GetRentalsRequest request) =>
        GetRentalsAsync(request, _rentalRepository.GetIncomingRentalsAsync);

    /// <inheritdoc/>
    public Task<RentalsListResponse> GetOutgoingRentalsAsync(GetRentalsRequest request) =>
        GetRentalsAsync(request, _rentalRepository.GetOutgoingRentalsAsync);

    private async Task<RentalsListResponse> GetRentalsAsync(
        GetRentalsRequest request,
        Func<int, Task<IEnumerable<DbRental>>> fetchRentals
    )
    {
        var currentUserId = GetCurrentUserId();
        var rentals = (await fetchRentals(currentUserId)).ToList();
        await ApplyAutomaticTransitionsAsync(rentals);
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

        await ApplyAutomaticTransitionsAsync([rental]);
        return ToRentalDetail(rental);
    }

    /// <inheritdoc/>
    public async Task<CreateRentalResponse> CreateRentalAsync(CreateRentalRequest request)
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

        return ToCreateRentalResponse(rental);
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

        await ApplyAutomaticTransitionsAsync([rental]);

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

    private async Task ApplyAutomaticTransitionsAsync(IList<DbRental> rentals)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        foreach (var rental in rentals)
        {
            RentalStatus? target = rental.Status switch
            {
                RentalStatus.OutForRent when rental.EndDate < today => RentalStatus.Overdue,
                RentalStatus.Requested when rental.StartDate < today => RentalStatus.Rejected,
                _ => null,
            };
            if (target is { } newStatus)
            {
                await _rentalRepository.UpdateRentalStatusAsync(rental.Id, newStatus);
                rental.Status = newStatus;
            }
        }
    }

    private int GetCurrentUserId() =>
        int.Parse(
            _tokenState.CurrentToken
                ?? throw new InvalidOperationException("No user is authenticated.")
        );

    private static RentalStatus ParseStatus(string status)
    {
        var normalized = status.Replace(" ", "");
        if (!Enum.TryParse<RentalStatus>(normalized, ignoreCase: true, out var result))
            throw new InvalidOperationException($"Unknown rental status: '{status}'.");
        return result;
    }

    private static RentalSummaryResponse ToRentalSummary(DbRental r) =>
        new(
            r.Id,
            r.ItemId,
            r.Item.Title,
            BorrowerId: r.BorrowerId,
            BorrowerName: $"{r.Borrower.FirstName} {r.Borrower.LastName}",
            BorrowerRating: null,
            OwnerId: r.OwnerId,
            OwnerName: $"{r.Owner.FirstName} {r.Owner.LastName}",
            OwnerRating: null,
            StartDate: r.StartDate.ToDateTime(TimeOnly.MinValue),
            EndDate: r.EndDate.ToDateTime(TimeOnly.MinValue),
            r.Status.ToString(),
            TotalPrice: r.Item.DailyRate * (r.EndDate.DayNumber - r.StartDate.DayNumber),
            RequestedAt: r.CreatedAt ?? DateTime.UtcNow
        );

    private static CreateRentalResponse ToCreateRentalResponse(DbRental r) =>
        new(
            r.Id,
            r.ItemId,
            r.Item.Title,
            r.BorrowerId,
            $"{r.Borrower.FirstName} {r.Borrower.LastName}",
            r.OwnerId,
            $"{r.Owner.FirstName} {r.Owner.LastName}",
            StartDate: r.StartDate.ToDateTime(TimeOnly.MinValue),
            EndDate: r.EndDate.ToDateTime(TimeOnly.MinValue),
            r.Status.ToString(),
            TotalPrice: r.Item.DailyRate * (r.EndDate.DayNumber - r.StartDate.DayNumber),
            CreatedAt: r.CreatedAt ?? DateTime.UtcNow
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
